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
            IEnumerable<Track> tracks = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged)
                .Select(obj => obj.Entity)
                .OfType<Track>()
                .Where(t => t.Path.CompareTo(filePath) == 0)
                .Union(from Track in db.Tracks
                       where Track.Path.CompareTo(filePath) == 0
                       select Track);

            if (tracks != null && tracks.Count() > 0)
            {
                // Update metadata
            }
            else
            {
                // Track is not in database
                TagLib.File file = TagLib.File.Create(filePath);

                // Add necessary artists
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
                HashSet<string> albumArtistNames = file.Tag.AlbumArtists
                    .Intersect(db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged)
                        .Select(obj => obj.Entity)
                        .OfType<Artist>()
                        .Union(from Artist in db.Artists
                               select Artist)
                        .Select(a => a.Name)
                        , StringComparer.Ordinal)
                    .ToHashSet(StringComparer.Ordinal);

                // matchingAlbumsInDatabase.Count() should be 0 or 1
                IEnumerable<Album> matchingAlbumsInDatabase = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added
                    | EntityState.Modified | EntityState.Unchanged)
                    .Select(obj => obj.Entity)
                    .OfType<Album>()
                    .Where(album => album.Title.CompareTo(file.Tag.Album) == 0)
                    .ToHashSet()
                    .Where(album => albumArtistNames.SetEquals(album.Artists.Select(artist => artist.Name)))
                    .Union(from Album in db.Albums
                           where Album.Title.CompareTo(file.Tag.Album) == 0
                           select Album)
                    .ToHashSet()
                    .Where(album => albumArtistNames.SetEquals(album.Artists.Select(artist => artist.Name)));

                Album trackAlbum = null;
                if (matchingAlbumsInDatabase.Count() == 0)
                {
                    trackAlbum = new Album
                    {
                        Title = file.Tag.Album
                    };

                    IEnumerable<Artist> albumArtists = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added |
                        EntityState.Modified | EntityState.Unchanged)
                        .Select(obj => obj.Entity)
                        .OfType<Artist>()
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

                // Add track
                // TODO: need decoder for length
                Track newTrack = new Track
                {
                    Path = filePath,
                    Title = file.Tag.Title,
                    Album = trackAlbum,
                    Duration = (Int64) file.Properties.Duration.TotalMilliseconds,
                    DateAdded = new DateTime(),
                    Bitrate = file.Properties.AudioBitrate,
                    ChannelCount = file.Properties.AudioChannels,
                    SampleRate = file.Properties.AudioSampleRate,
                    BitsPerSample = file.Properties.BitsPerSample,
                    Codec = file.Properties.Codecs.First().Description,
                    Playcount = 0,
                    Size = file.Length,
                    TrackNumber = file.Tag.Track,
                    Date = (file.Tag.Year == 0) ? (DateTime?)null : new DateTime((int)file.Tag.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    MusicBrainzId = file.Tag.MusicBrainzTrackId, 
                    BPM = file.Tag.BeatsPerMinute == 0 ? (uint?) null : file.Tag.BeatsPerMinute
                };

                IEnumerable<Artist> trackArtists = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added |
                        EntityState.Modified | EntityState.Unchanged)
                        .Select(obj => obj.Entity)
                        .OfType<Artist>()
                        .Where(artist => file.Tag.Performers.Any(s => s.CompareTo(artist.Name) == 0));

                foreach (Artist trackArtist in trackArtists)
                    newTrack.Artists.Add(trackArtist);

                IEnumerable<Genre> trackGenres = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added |
                        EntityState.Modified | EntityState.Unchanged)
                        .Select(obj => obj.Entity)
                        .OfType<Genre>()
                        .Where(g => file.Tag.Genres.Contains(g.Name, StringComparer.Ordinal));

                foreach (Genre genre in trackGenres)
                    newTrack.Genres.Add(genre);

                db.Tracks.AddObject(newTrack);
            }
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
