using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Data.Common;
using System.Data.Objects;
using System.Transactions;
using System.Data.SQLite;
using System.Data;
using System.Security.Cryptography;
using Howler.Util;
using Howler.Core.Tagging;
using TagLib;

namespace Howler.Core.Database
{
    class Collection
    {
        public Collection()
        {
            if (System.IO.File.Exists("..\\howler.db")) 
                return;

            string fileName = "..\\howler.db";
            SQLiteConnection.CreateFile(fileName);
            SQLiteConnection conn = new SQLiteConnection();
            conn.ConnectionString = new DbConnectionStringBuilder
            {
                {"Data Source", fileName},
                {"Version", "3"},
                {"FailIfMissing", "False"},
            }.ConnectionString;
            conn.Open();

            FileInfo file = new FileInfo("C:\\Users\\Greg\\documents\\visual studio 2010\\Projects\\Howler\\Howler\\Core\\Database\\Collection.edmx.sql");
            string script = file.OpenText().ReadToEnd();

            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = script;
            cmd.ExecuteNonQuery();

            conn.Close();
            conn.Dispose();
        }

        public void ImportDirectory(String path)
        {
            if (!Directory.Exists(path)) 
                return;

            string[] extensions = { ".mp3", ".m4a", ".flac" };
            IEnumerable<string> newFiles = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Where(s => extensions.Any(ext => ext == Path.GetExtension(s)));

            using (CollectionContainer db = new CollectionContainer())
            {
                using (TransactionScope scope = new TransactionScope())
                {

                    db.Connection.Open();

                    foreach (string file in newFiles)
                    {
                        ImportFile(file, db);
                    }

                    try
                    {
                        db.SaveChanges(SaveOptions.None);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        scope.Complete();
                        db.AcceptAllChanges();
                        db.Connection.Close();
                    }
                }
            }
        }

        private void ImportFile(string filePath, CollectionContainer db)
        {
            TagLib.File file = TagLib.File.Create(filePath);
            TagLib.Properties properties = file.Properties;
            Tag tag = file.Tag;

            string[] tagLibStrings = 
                {   file.Length.ToString(CultureInfo.InvariantCulture),
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
            
            IEnumerable<Track> tracksWithPath = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged)
                .Select(obj => obj.Entity)
                .OfType<Track>()
                .Where(t => System.String.Compare(t.Path, filePath, System.StringComparison.Ordinal) == 0)
                .Union(from track in db.Tracks
                       where System.String.Compare(track.Path, filePath, System.StringComparison.Ordinal) == 0
                       select track)
                .ToList();

            if (tracksWithPath.Any())
            {
                // File already exists in database
                IEnumerable<Track> tracksWithPathAndHash = tracksWithPath.Where(t => String.Compare(t.TagLibHash, tagLibHash, StringComparison.Ordinal) == 0).ToList();
                if (!tracksWithPathAndHash.Any())
                    UpdateTrackInDatabase(filePath, file, db, tagLibHash, tracksWithPath.First());
            }
            else
                AddTrackToDatabase(filePath, file, db, tagLibHash);
        }

        private void AddTrackToDatabase(string filePath, TagLib.File file, CollectionContainer db, string tagLibHash)
        {
            Album trackAlbum = addNonTrackEntitiesAndReturnAlbum(file, db);

            Track newTrack = new Track
            {
                Path = filePath,
                Title = file.Tag.Title,
                Album = trackAlbum,
                Duration = (Int64)file.Properties.Duration.TotalMilliseconds,
                DateAdded = new DateTime(),
                Bitrate = file.Properties.AudioBitrate,
                ChannelCount = file.Properties.AudioChannels,
                SampleRate = file.Properties.AudioSampleRate,
                BitsPerSample = file.Properties.BitsPerSample,
                Codec = file.Properties.Codecs.First().Description,
                Playcount = 0,
                Size = file.Length,
                TagLibHash = tagLibHash,
                TrackNumber = file.Tag.Track,
                Date = file.Tag.GetDate(),
                MusicBrainzId = file.Tag.MusicBrainzTrackId,
                BPM = file.Tag.BeatsPerMinute == 0 ? (uint?)null : file.Tag.BeatsPerMinute,
                Rating = file.Tag.GetRating()
            };

            IEnumerable<Artist> trackArtists = GetTrackArtistsForTag(db, file.Tag); 

            foreach (Artist trackArtist in trackArtists)
                newTrack.Artists.Add(trackArtist);

            IEnumerable<Genre> trackGenres = GetTrackGenresForTag(db, file.Tag);

            foreach (Genre genre in trackGenres)
                newTrack.Genres.Add(genre);

            file.Dispose();

            db.Tracks.AddObject(newTrack);
        }

        private void UpdateTrackInDatabase(string filePath, TagLib.File file, CollectionContainer db, string tagLibHash, Track track)
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
            track.Duration = (Int64)file.Properties.Duration.TotalMilliseconds;
            track.Bitrate = file.Properties.AudioBitrate;
            track.ChannelCount = file.Properties.AudioChannels;
            track.SampleRate = file.Properties.AudioSampleRate;
            track.BitsPerSample = file.Properties.BitsPerSample;
            track.Codec = file.Properties.Codecs.First().Description;
            track.Size = file.Length;
            track.TagLibHash = tagLibHash;
            track.TrackNumber = file.Tag.Track;
            track.Date = file.Tag.GetDate();
            track.MusicBrainzId = file.Tag.MusicBrainzTrackId;
            track.BPM = file.Tag.BeatsPerMinute == 0 ? (uint?)null : file.Tag.BeatsPerMinute;
            track.Rating = file.Tag.GetRating();

            foreach (Artist trackArtist in artistsToRemove)
                track.Artists.Remove(trackArtist);

            foreach (Artist trackArtist in artistsToAdd)
                track.Artists.Add(trackArtist);

            foreach (Genre genre in genresToRemove)
                track.Genres.Remove(genre);

            foreach (Genre genre in genresToAdd)
                track.Genres.Add(genre);
        }

        private IEnumerable<Genre> GetTrackGenresForTag(CollectionContainer db, Tag tag)
        {
            return db.ObjectStateManager.GetObjectStateEntries(EntityState.Added |
                     EntityState.Modified | EntityState.Unchanged)
                     .Select(obj => obj.Entity)
                     .OfType<Genre>()
                     .Where(g => tag.Genres.Any(s => String.Compare(g.Name, s, StringComparison.Ordinal) == 0))
                     .Union(from genre in db.Genres
                            where tag.Genres.Any(s => String.Compare(genre.Name, s, StringComparison.Ordinal) == 0)
                            select genre);
        }

        private IEnumerable<Artist> GetTrackArtistsForTag(CollectionContainer db, Tag tag)
        {
            return db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | 
                EntityState.Modified | EntityState.Unchanged)
                .Select(obj => obj.Entity)
                .OfType<Artist>()
                .Where(artist => tag.Performers.Any(s => String.Compare(s, artist.Name, StringComparison.Ordinal) == 0))
                .Union(from artist in db.Artists
                       select artist);
        }

        private Album addNonTrackEntitiesAndReturnAlbum(TagLib.File file, CollectionContainer db)
        {
            IEnumerable<string> artistsToAdd = file.Tag.AlbumArtists
                    .Union(file.Tag.Performers)
                    .Except(db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged)
                        .Select(obj => obj.Entity)
                        .OfType<Artist>()
                        .Union(from artist in db.Artists
                               select artist)
                        .Select(a => a.Name)
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

                    db.Artists.AddObject(newArtist);
                }
            }

            // Add album if necessary
            string orderedAlbumArtists = String.Concat(file.Tag.AlbumArtists.OrderBy(s => s, StringComparer.Ordinal));
            MD5 albumMd5 = MD5.Create();
            string albumArtistsHash = albumMd5.GetMd5Hash(orderedAlbumArtists);

            // matchingAlbumsInDatabase.Count() should be 0 or 1
            IEnumerable<Album> matchingAlbumsInDatabase = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added
                | EntityState.Modified | EntityState.Unchanged)
                .Select(obj => obj.Entity)
                .OfType<Album>()
                .Where(album => String.Compare(album.Title, file.Tag.Album, StringComparison.Ordinal) == 0 
                    && String.Compare(album.ArtistsHash, albumArtistsHash, StringComparison.Ordinal) == 0)
                .Union(from album in db.Albums
                       where String.Compare(album.Title, file.Tag.Album, StringComparison.Ordinal) == 0
                       && String.Compare(album.ArtistsHash, albumArtistsHash, StringComparison.Ordinal) == 0
                       select album)
                .ToList();

            Album trackAlbum;
            if (!matchingAlbumsInDatabase.Any())
            {
                trackAlbum = new Album
                {
                    Title = file.Tag.Album,
                    ArtistsHash = albumArtistsHash
                };

                IEnumerable<Artist> albumArtists = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added |
                    EntityState.Modified | EntityState.Unchanged)
                    .Select(obj => obj.Entity)
                    .OfType<Artist>()
                    .Union(from artist in db.Artists
                           select artist)
                    .Where(artist => file.Tag.AlbumArtists.Any(s => String.Compare(s, artist.Name, StringComparison.Ordinal) == 0));

                foreach (Artist albumArtist in albumArtists)
                {
                    trackAlbum.Artists.Add(albumArtist);
                }

                db.Albums.AddObject(trackAlbum);
            }
            else
            {
                trackAlbum = matchingAlbumsInDatabase.First();
            }

            // Add genres if necessary
            IEnumerable<string> genreNamesToAdd = file.Tag.Genres.Except(db.ObjectStateManager.GetObjectStateEntries(EntityState.Added |
                    EntityState.Modified | EntityState.Unchanged)
                    .Select(obj => obj.Entity)
                    .OfType<Genre>()
                    .Select(g => g.Name), StringComparer.Ordinal);

            foreach (string genreName in genreNamesToAdd)
            {
                Genre genre = new Genre
                {
                    Name = genreName
                };

                db.Genres.AddObject(genre);
            }

            return trackAlbum;
        }

        public IEnumerable<Track> GetTracks()
        {
            CollectionContainer db = new CollectionContainer();
            IEnumerable<Track> tracks = from track in db.Tracks
                                        select track;
            return tracks;
        }
    }
}
