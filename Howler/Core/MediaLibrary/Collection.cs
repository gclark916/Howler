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
            bool fileExists = System.IO.File.Exists(DbFile);

            // Only apply schema if file does not exist
            new SchemaExport(config).Create(false, !fileExists);
        }

        public void ImportDirectory(String path)
        {
            if (!Directory.Exists(path))
                return;

            string[] extensions = { ".mp3", ".m4a", ".flac" };
            IEnumerable<string> newFiles = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Where(s => extensions.Any(ext => String.Compare(ext, Path.GetExtension(s), StringComparison.OrdinalIgnoreCase) == 0));

            var session = _session;
            using (var scope = session.BeginTransaction())
            {
                int fileCount = 1;
                var enumerable = newFiles as string[] ?? newFiles.ToArray();
                DateTime startTime = DateTime.Now, importTime = DateTime.Now;
                var tracksNotAdded = new List<string>();
                foreach (string file in enumerable)
                {
                    if (fileCount%20 == 0)
                    {
                        var span = DateTime.Now.Subtract(startTime);
                        Console.WriteLine("Importing file {0}/{1} {2}", fileCount, enumerable.Count(), span.TotalMilliseconds);
                        session.Flush();
                        session.Clear();
                        startTime = DateTime.Now;
                    }
                    try
                    {
                        ImportFile(file, session);
                    }
                    catch (Exception)
                    {
                        tracksNotAdded.Add(file);
                    }
                    fileCount++;
                }

                Console.WriteLine("Imported file {0}/{1} {2} seconds", fileCount, enumerable.Count(), DateTime.Now.Subtract(importTime).TotalSeconds);

                if (tracksNotAdded.Any())
                    Console.WriteLine("{0} tracks not added");

                try
                {
                    scope.Commit();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private void ImportFile(string filePath, ISession session)
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
                    (from track in session.Query<Track>()
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
                        UpdateTrackInDatabase(filePath, file, session, tagLibHash, tracksWithPath.First());
                }
                else
                    AddTrackToDatabase(filePath, file, session, tagLibHash);
            }
        }

        private void AddTrackToDatabase(string filePath, TagLib.File file, ISession session, string tagLibHash)
        {
            Album trackAlbum = AddNonTrackEntitiesAndReturnAlbum(file, session);
            var trackArtists = GetTrackArtistsForTag(session, file.Tag);
            var trackGenres = GetTrackGenresForTag(session, file.Tag);

            FileInfo fileInfo = new FileInfo(filePath);
            Track newTrack = new Track
            {
                Album = trackAlbum,
                Artists = new HashedSet<Artist>(trackArtists.ToArray()),
                Bitrate = (uint) file.Properties.AudioBitrate,
                BitsPerSample = (uint) file.Properties.BitsPerSample,
                Bpm = file.Tag.BeatsPerMinute == 0 ? (uint?)null : file.Tag.BeatsPerMinute,
                ChannelCount = (uint) file.Properties.AudioChannels,
                Codec = file.Properties.Codecs.First().Description,
                Date = file.Tag.GetDate(),
                DateAdded = DateTime.Now,
                DiscNumber = file.Tag.Disc > 0 ? (uint?)file.Tag.Disc : null,
                Duration = (UInt64)file.Properties.Duration.TotalMilliseconds,
                Genres = new HashedSet<Genre>(trackGenres.ToArray()),
                MusicBrainzId = file.Tag.MusicBrainzTrackId,
                Path = filePath,
                Playcount = 0,
                Rating = file.Tag.GetRatingNullableUInt32(),
                SampleRate = (uint) file.Properties.AudioSampleRate,
                Size = (ulong) fileInfo.Length,
                TagLibHash = tagLibHash,
                Title = file.Tag.Title,
                TrackNumber = file.Tag.Track > 0 ? (uint?)file.Tag.Track : null,
            };

            session.Save(newTrack);
        }

        private void UpdateTrackInDatabase(string filePath, TagLib.File file, ISession session, string tagLibHash, Track track)
        {
            Album trackAlbum = AddNonTrackEntitiesAndReturnAlbum(file, session);

            IEnumerable<Artist> newTrackArtists = GetTrackArtistsForTag(session, file.Tag);

            IEnumerable<Genre> newTrackGenres = GetTrackGenresForTag(session, file.Tag);

            track.Artists = new HashedSet<Artist>(newTrackArtists.ToArray());
            track.Genres = new HashedSet<Genre>(newTrackGenres.ToArray());
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

            session.Update(track);
        }

        private IEnumerable<Genre> GetTrackGenresForTag(ISession session, Tag tag)
        {
            // All these ToArrays are necessary because things seem to screw up if a linq Ienumerable is used in NHibernate's linq query
            var tagGenres = tag.Genres.Select(s => s.TrimEnd('\0')).ToArray();

            var genres = (from genre in session.Query<Genre>()
                          where tagGenres.Contains(genre.Name)
                         select genre);

            return genres;
        }

        private IEnumerable<Artist> GetTrackArtistsForTag(ISession session, Tag tag)
        {
            var tagArtists = tag.Performers.Select(s => s.TrimEnd('\0')).ToArray();

            var artists = (from artist in session.Query<Artist>()
                           where tagArtists.Contains(artist.Name)
                          select artist);

            return artists;
        }

        private Album AddNonTrackEntitiesAndReturnAlbum(TagLib.File file, ISession session)
        {
            var artistsToAdd = file.Tag.AlbumArtists
                    .Union(file.Tag.Performers)
                    .Select(s => s.TrimEnd('\0'))
                    .ToArray()
                    .Except(from artist in session.Query<Artist>()
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

                    session.Save(newArtist);
                }
            }

            // Add album if necessary
            var tagAlbumArtists = file.Tag.AlbumArtists.Select(s => s.TrimEnd('\0')).ToArray();

            var albumArtistsQueryable =
                (from artist in session.Query<Artist>()
                 where tagAlbumArtists.Contains(artist.Name)
                 select artist)
                 .ToList();

            var albumArtists = new HashedSet<Artist>(albumArtistsQueryable);
            string albumArtistsHash = Album.ComputeArtistsHash(albumArtists);

            // matchingAlbumsInDatabase.Count() should be 0 or 1
            var tagAlbum = file.Tag.Album.TrimEnd('\0');

            var matchingAlbumsInDatabase = 
                (from album in session.Query<Album>()
                 where album.Title == tagAlbum
                 && album.ArtistsHash == albumArtistsHash
                 select album);

            Album trackAlbum;
            if (!matchingAlbumsInDatabase.Any())
            {
                trackAlbum = new Album
                    {
                        Title = tagAlbum,
                        Artists = albumArtists
                    };

                session.Save(trackAlbum);
            }
            else
            {
                trackAlbum = matchingAlbumsInDatabase.First();
            }

            // Add genres if necessary
            var genreNamesToAdd = file.Tag.Genres.Select(s => s.TrimEnd('\0')).Except(
                from genre in session.Query<Genre>()
                select genre.Name, StringComparer.Ordinal);

            foreach (string genreName in genreNamesToAdd)
            {
                Genre genre = new Genre
                {
                    Name = genreName
                };

                session.Save(genre);
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
