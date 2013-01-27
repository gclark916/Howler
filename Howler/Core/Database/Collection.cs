using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
using System.Data.Objects;
using System.Transactions;
using System.Data.SQLite;
using System.Data;
using System.Security.Cryptography;
using Howler.Core.Database;
using Howler.Util;
using TagLib;

namespace Howler.Core.Database
{
    class Collection
    {
        public Collection()
        {
            if (!System.IO.File.Exists("..\\howler.db"))
            {
                
                string fileName = "..\\howler.db";
                SQLiteConnection.CreateFile(fileName);
                SQLiteConnection conn = new SQLiteConnection();
                conn.ConnectionString = new DbConnectionStringBuilder()
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
            }
        }

        public void ImportDirectory(String path)
        {
            if (Directory.Exists(path))
            {
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
        }

        private void ImportFile(string filePath, CollectionContainer db)
        {

            TagLib.File file = TagLib.File.Create(filePath);
            TagLib.Properties properties = file.Properties;
            TagLib.Tag tag = file.Tag;
            string[] tagLibStrings = 
                {   file.Length.ToString(),
                    properties.AudioBitrate.ToString(),
                    properties.AudioChannels.ToString(),
                    properties.AudioSampleRate.ToString(),
                    properties.BitsPerSample.ToString(),
                    String.Concat(properties.Codecs),
                    tag.Album,
                    String.Concat(tag.AlbumArtists),
                    tag.BeatsPerMinute.ToString(),
                    tag.Comment,
                    tag.Disc.ToString(),
                    tag.DiscCount.ToString(),
                    String.Concat(tag.Genres),
                    tag.Lyrics,
                    tag.MusicBrainzArtistId,
                    tag.MusicBrainzDiscId,
                    tag.MusicBrainzReleaseArtistId,
                    tag.MusicBrainzReleaseId,
                    String.Concat(tag.Performers),
                    tag.Title,
                    tag.Track.ToString(),
                    tag.TrackCount.ToString(),
                    tag.Year.ToString() };

            MD5 md5 = System.Security.Cryptography.MD5.Create();
            string tagLibHash = md5.GetMd5Hash(String.Concat(tagLibStrings));
            
            IEnumerable<Track> tracksWithPath = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged)
                .Select(obj => obj.Entity)
                .OfType<Track>()
                .Where(t => t.Path.CompareTo(filePath) == 0)
                .Union(from Track in db.Tracks
                       where Track.Path.CompareTo(filePath) == 0
                       select Track);

            if (tracksWithPath.Count() > 0)
            {
                // File already exists in database
                IEnumerable<Track> tracksWithPathAndHash = tracksWithPath.Where(t => t.TagLibHash.CompareTo(tagLibHash) == 0);
                if (tracksWithPathAndHash.Count() > 0)
                    // Hashes match, no updating needed
                    return;
                else
                    updateTrackInDatabase(filePath, file, db, tagLibHash, tracksWithPathAndHash.First());
            }
            else
                addTrackToDatabase(filePath, file, db, tagLibHash);
        }

        private void addTrackToDatabase(string filePath, TagLib.File file, CollectionContainer db, string tagLibHash)
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
                Date = (file.Tag.Year == 0) ? (DateTime?)null : new DateTime((int)file.Tag.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                MusicBrainzId = file.Tag.MusicBrainzTrackId,
                BPM = file.Tag.BeatsPerMinute == 0 ? (uint?)null : file.Tag.BeatsPerMinute
            };

            IEnumerable<Artist> trackArtists = GetTrackArtistsForTag(db, file.Tag); 

            foreach (Artist trackArtist in trackArtists)
                newTrack.Artists.Add(trackArtist);

            IEnumerable<Genre> trackGenres = GetTrackGenresForTag(db, file.Tag);

            foreach (Genre genre in trackGenres)
                newTrack.Genres.Add(genre);

            db.Tracks.AddObject(newTrack);
        }

        private void updateTrackInDatabase(string filePath, TagLib.File file, CollectionContainer db, string tagLibHash, Track track)
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
            track.DateAdded = new DateTime();
            track.Bitrate = file.Properties.AudioBitrate;
            track.ChannelCount = file.Properties.AudioChannels;
            track.SampleRate = file.Properties.AudioSampleRate;
            track.BitsPerSample = file.Properties.BitsPerSample;
            track.Codec = file.Properties.Codecs.First().Description;
            track.Playcount = 0;
            track.Size = file.Length;
            track.TagLibHash = tagLibHash;
            track.TrackNumber = file.Tag.Track;
            track.Date = (file.Tag.Year == 0) ? (DateTime?)null : new DateTime((int)file.Tag.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            track.MusicBrainzId = file.Tag.MusicBrainzTrackId;
            track.BPM = file.Tag.BeatsPerMinute == 0 ? (uint?)null : file.Tag.BeatsPerMinute;

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
                     .Where(g => tag.Genres.Contains(g.Name, StringComparer.Ordinal))
                     .Union(from Genre in db.Genres
                            where tag.Genres.Contains(Genre.Name, StringComparer.Ordinal)
                            select Genre);
        }

        private IEnumerable<Artist> GetTrackArtistsForTag(CollectionContainer db, Tag tag)
        {
            return db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | 
                EntityState.Modified | EntityState.Unchanged)
                .Select(obj => obj.Entity)
                .OfType<Artist>()
                .Where(artist => tag.Performers.Any(s => s.CompareTo(artist.Name) == 0))
                .Union(from Artist in db.Artists
                       select Artist);
        }

        private Album addNonTrackEntitiesAndReturnAlbum(TagLib.File file, CollectionContainer db)
        {
            IEnumerable<string> artistsToAdd = file.Tag.AlbumArtists
                    .Union(file.Tag.Performers)
                    .Except(db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged)
                        .Select(obj => obj.Entity)
                        .OfType<Artist>()
                        .Union(from Artist in db.Artists
                               select Artist)
                        .Select(a => a.Name)
                        , StringComparer.Ordinal);

            if (artistsToAdd.Count() > 0)
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
            MD5 albumMd5 = System.Security.Cryptography.MD5.Create();
            string albumArtistsHash = albumMd5.GetMd5Hash(orderedAlbumArtists);

            // matchingAlbumsInDatabase.Count() should be 0 or 1
            IEnumerable<Album> matchingAlbumsInDatabase = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added
                | EntityState.Modified | EntityState.Unchanged)
                .Select(obj => obj.Entity)
                .OfType<Album>()
                .Where(album => album.Title.CompareTo(file.Tag.Album) == 0 && album.ArtistsHash.CompareTo(albumArtistsHash) == 0)
                .Union(from Album in db.Albums
                       where Album.Title.CompareTo(file.Tag.Album) == 0
                       && Album.ArtistsHash.CompareTo(albumArtistsHash) == 0
                       select Album);

            Album trackAlbum = null;
            if (matchingAlbumsInDatabase.Count() == 0)
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
                    .Union(from Artist in db.Artists
                           select Artist)
                    .Where(artist => file.Tag.AlbumArtists.Any(s => s.CompareTo(artist.Name) == 0));

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
            IEnumerable<Track> tracks = from Track in db.Tracks
                                        select Track;
            return tracks;
        }
    }
}
