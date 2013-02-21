using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Howler.Core.MediaLibrary.Entities;
using Howler.Core.Tagging;
using Howler.Util;
using Iesi.Collections.Generic;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using TagLib;
using NHibernate.Linq;

namespace Howler.Core.MediaLibrary
{
    class Collection
    {
        private const string DbFile = "..\\Howler2.db";
        private readonly ISessionFactory _sessionFactory;
        private readonly ISession _session;

        public Collection()
        {
            _sessionFactory = CreateSessionFactory();
            _session = _sessionFactory.OpenSession();
        }

        private static ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard
                    .UsingFile(DbFile))
                .Mappings(m =>
                    m.FluentMappings.AddFromAssemblyOf<Collection>())
                .ExposeConfiguration(BuildSchema)
                .BuildSessionFactory();
        }

        private static void BuildSchema(Configuration config)
        {
            // delete the existing db on each run
            //if (File.Exists(DbFile))
            //    File.Delete(DbFile);

            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
            new SchemaExport(config)
                .Create(false, false);
        }

        public void ImportDirectory(String path)
        {
            if (!Directory.Exists(path))
                return;

            string[] extensions = { ".mp3", ".m4a", ".flac" };
            IEnumerable<string> newFiles = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Where(s => extensions.Any(ext => String.Compare(ext, Path.GetExtension(s), StringComparison.OrdinalIgnoreCase) == 0));

            var db = _session;
            {
                using (var scope = db.BeginTransaction())
                {
                    int fileCount = 1;
                    var enumerable = newFiles as string[] ?? newFiles.ToArray();
                    foreach (string file in enumerable)
                    {
                        if (fileCount%20 == 0)
                        {
                            db.Flush();
                            db.Clear();
                        }
                        var startTime = DateTime.Now;
                        ImportFile(file, db);
                        var span = DateTime.Now.Subtract(startTime);
                        Console.WriteLine("Imported file {0}/{1} {2}", fileCount++, enumerable.Count(), span.TotalMilliseconds);
                    }

                    try
                    {
                        scope.Commit();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        //scope.Commit();
                        //db.Flush();
                        //db.Connection.Close();
                    }
                }
            }
        }

        private void ImportFile(string filePath, ISession db)
        {
            using (var file = TagLib.File.Create(filePath))
            {
                TagLib.Properties properties = file.Properties;
                Tag tag = file.Tag;

                string[] tagLibStrings =
                    {
                        file.Length.ToString(CultureInfo.InvariantCulture),
                        properties.AudioBitrate.ToString(CultureInfo.InvariantCulture),
                        properties.AudioChannels.ToString(CultureInfo.InvariantCulture),
                        properties.AudioSampleRate.ToString(CultureInfo.InvariantCulture),
                        properties.BitsPerSample.ToString(CultureInfo.InvariantCulture),
                        String.Concat(properties.Codecs),
                        tag.Album,
                        String.Concat(tag.AlbumArtists),
                        tag.BeatsPerMinute.ToString(CultureInfo.InvariantCulture),
                        tag.Comment,
                        tag.Disc.ToString(CultureInfo.InvariantCulture),
                        tag.DiscCount.ToString(CultureInfo.InvariantCulture),
                        String.Concat(tag.Genres),
                        tag.Lyrics,
                        tag.MusicBrainzArtistId,
                        tag.MusicBrainzDiscId,
                        tag.MusicBrainzReleaseArtistId,
                        tag.MusicBrainzReleaseId,
                        String.Concat(tag.Performers),
                        tag.Title,
                        tag.Track.ToString(CultureInfo.InvariantCulture),
                        tag.TrackCount.ToString(CultureInfo.InvariantCulture),
                        tag.GetDate(),
                        tag.GetRating().ToString(CultureInfo.InvariantCulture)
                    };

                MD5 md5 = MD5.Create();
                string tagLibHash = md5.GetMd5Hash(String.Concat(tagLibStrings));

                IEnumerable<Track> tracksWithPath =
                    (from track in db.Query<Track>()
                     where track.Path == filePath
                     select track)
                     .ToList();

                if (tracksWithPath.Any())
                {
                    // File already exists in database
                    IEnumerable<Track> tracksWithPathAndHash = tracksWithPath
                        .Where(t => String.Compare(t.TagLibHash, tagLibHash, StringComparison.Ordinal) == 0)
                        .ToList();
                    if (!tracksWithPathAndHash.Any())
                        UpdateTrackInDatabase(filePath, file, db, tagLibHash, tracksWithPath.First());
                }
                else
                    AddTrackToDatabase(filePath, file, db, tagLibHash);
            }
        }

        private void AddTrackToDatabase(string filePath, TagLib.File file, ISession db, string tagLibHash)
        {
            Album trackAlbum = addNonTrackEntitiesAndReturnAlbum(file, db);

            Track newTrack = new Track
            {
                Path = filePath,
                Title = file.Tag.Title,
                Album = trackAlbum,
                Duration = (UInt64)file.Properties.Duration.TotalMilliseconds,
                DateAdded = DateTime.Now,
                Bitrate = (uint) file.Properties.AudioBitrate,
                ChannelCount = (uint) file.Properties.AudioChannels,
                SampleRate = (uint) file.Properties.AudioSampleRate,
                BitsPerSample = (uint) file.Properties.BitsPerSample,
                Codec = file.Properties.Codecs.First().Description,
                Playcount = 0,
                Size = (ulong) file.Length,
                TagLibHash = tagLibHash,
                TrackNumber = file.Tag.Track,
                Date = file.Tag.GetDate(),
                MusicBrainzId = file.Tag.MusicBrainzTrackId,
                Bpm = file.Tag.BeatsPerMinute == 0 ? (uint?)null : file.Tag.BeatsPerMinute,
                Rating = file.Tag.GetRatingNullableUInt32()
            };

            IEnumerable<Artist> trackArtists = GetTrackArtistsForTag(db, file.Tag);

            foreach (Artist trackArtist in trackArtists)
            {
                newTrack.Artists.Add(trackArtist);
                trackArtist.Tracks.Add(newTrack);
                db.Update(trackArtist);
            }

            IEnumerable<Genre> trackGenres = GetTrackGenresForTag(db, file.Tag);

            foreach (Genre genre in trackGenres)
            {
                newTrack.Genres.Add(genre);
                genre.Tracks.Add(newTrack);
                db.Update(genre);
            }

            file.Dispose();

            db.Save(newTrack);
        }

        private void UpdateTrackInDatabase(string filePath, TagLib.File file, ISession db, string tagLibHash, Track track)
        {
            Album trackAlbum = addNonTrackEntitiesAndReturnAlbum(file, db);

            IEnumerable<Artist> newTrackArtists = GetTrackArtistsForTag(db, file.Tag);
            IEnumerable<Artist> artistsToRemove = track.Artists.Where(a => !newTrackArtists.Contains(a));
            IEnumerable<Artist> artistsToAdd = newTrackArtists.Where(a => !track.Artists.Contains(a));

            IEnumerable<Genre> newTrackGenres = GetTrackGenresForTag(db, file.Tag);
            IEnumerable<Genre> genresToRemove = track.Genres.Where(g => !newTrackGenres.Contains(g));
            IEnumerable<Genre> genresToAdd = newTrackGenres.Where(g => !track.Genres.Contains(g));

            track.Path = filePath;
            track.Title = file.Tag.Title;
            track.Album = trackAlbum;
            track.Duration = (ulong) file.Properties.Duration.TotalMilliseconds;
            track.Bitrate = (uint) file.Properties.AudioBitrate;
            track.ChannelCount = (uint) file.Properties.AudioChannels;
            track.SampleRate = (uint) file.Properties.AudioSampleRate;
            track.BitsPerSample = (uint) file.Properties.BitsPerSample;
            track.Codec = file.Properties.Codecs.First().Description;
            track.Size = (ulong) file.Length;
            track.TagLibHash = tagLibHash;
            track.TrackNumber = file.Tag.Track;
            track.Date = file.Tag.GetDate();
            track.MusicBrainzId = file.Tag.MusicBrainzTrackId;
            track.Bpm = file.Tag.BeatsPerMinute == 0 ? (uint?)null : file.Tag.BeatsPerMinute;
            track.Rating = file.Tag.GetRatingNullableUInt32();

            foreach (Artist trackArtist in artistsToRemove)
            {
                trackArtist.Tracks.Remove(track);
                db.Update(trackArtist);
                track.Artists.Remove(trackArtist);
            }

            foreach (Artist trackArtist in artistsToAdd)
            {
                trackArtist.Tracks.Add(track);
                db.Update(trackArtist);
                track.Artists.Add(trackArtist);
            }

            foreach (Genre genre in genresToRemove)
            {
                genre.Tracks.Remove(track);
                db.Update(genre);
                track.Genres.Remove(genre);
            }

            foreach (Genre genre in genresToAdd)
            {
                genre.Tracks.Add(track);
                db.Update(genre);
                track.Genres.Add(genre);
            }
        }

        private IEnumerable<Genre> GetTrackGenresForTag(ISession db, Tag tag)
        {
            var genres = (from genre in db.Query<Genre>()
                         where tag.Genres.Contains(genre.Name)
                         select genre)
                         .Fetch(g => g.Tracks);

            return genres;
        }

        private IEnumerable<Artist> GetTrackArtistsForTag(ISession db, Tag tag)
        {
            var artists = (from artist in db.Query<Artist>()
                          where tag.Performers.Contains(artist.Name)
                          select artist)
                          .Fetch(a => a.Tracks);

            return artists;
        }

        private Album addNonTrackEntitiesAndReturnAlbum(TagLib.File file, ISession db)
        {
            IEnumerable<string> artistsToAdd = file.Tag.AlbumArtists
                    .Union(file.Tag.Performers)
                    .Except(from artist in db.Query<Artist>()
                            select artist.Name
                        , StringComparer.Ordinal)
                    .ToList();

            if (artistsToAdd.Any())
            {
                foreach (string artistName in artistsToAdd)
                {
                    Artist newArtist = new Artist
                    {
                        Name = artistName
                    };

                    db.Save(newArtist);
                }
            }

            // Add album if necessary
            var albumArtistsQueryable =
                (from artist in db.Query<Artist>()
                 where file.Tag.AlbumArtists.Contains(artist.Name)
                 select artist)
                 .Fetch(a => a.Albums)
                 .ToList();

            //var albumArtistsHashSet = albumArtistsQueryable.ToHashSet();
            var albumArtists = new HashedSet<Artist>(albumArtistsQueryable);

            string albumArtistsHash = Album.ComputeArtistsHash(albumArtists);

            // matchingAlbumsInDatabase.Count() should be 0 or 1
            IEnumerable<Album> matchingAlbumsInDatabase = 
                (from album in db.Query<Album>()
                 where album.Title == file.Tag.Album
                 && album.ArtistsHash == albumArtistsHash
                 select album)
                 .ToList();

            Album trackAlbum;
            if (!matchingAlbumsInDatabase.Any())
            {
                trackAlbum = new Album
                    {
                        Title = file.Tag.Album,
                        Artists = albumArtists
                    };

                foreach (Artist artist in albumArtists)
                {
                    artist.Albums.Add(trackAlbum);
                    db.Update(artist);
                }

                db.Save(trackAlbum);
            }
            else
            {
                trackAlbum = matchingAlbumsInDatabase.First();
            }

            // Add genres if necessary
            IEnumerable<string> genreNamesToAdd = file.Tag.Genres.Except(
                from genre in db.Query<Genre>()
                select genre.Name, StringComparer.Ordinal);

            foreach (string genreName in genreNamesToAdd)
            {
                Genre genre = new Genre
                {
                    Name = genreName
                };

                db.Save(genre);
            }

            return trackAlbum;
        }

        public IEnumerable<Track> GetTracks()
        {
            var session = _session;
            using (session.BeginTransaction())
            {
                var tracks = 
                    (from track in session.Query<Track>()
                        select track);

                return tracks;
            }
        }

        public IEnumerable<Artist> GetTrackArtistsAndAlbumArtists()
        {
            var session = _session;
            using (session.BeginTransaction())
            {
                var artists = 
                    from artist in session.Query<Artist>()
                    where artist.Tracks.Any() || artist.Albums.Any()
                    orderby artist.Name
                    select artist;

                return artists;
            }
        }
    }
}
