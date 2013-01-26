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
                conn.Dispose();
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
            IEnumerable<Track> tracks = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added 
                | EntityState.Modified | EntityState.Unchanged)
                .Select(obj => obj.Entity)
                .OfType<Track>()
                .Where((t) => t.Path.CompareTo(filePath) == 0);

            if (tracks != null && tracks.Count() > 0)
            {
                // Update metadata
            }
            else
            {
                // Track is not in database
                TagLib.File file = TagLib.File.Create(filePath);

                // Add necessary artists
                IEnumerable<string> albumArtistsAlreadyInDatabase = file.Tag.AlbumArtists
                    .Intersect(db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged)
                    .Select(obj => obj.Entity)
                    .OfType<Artist>()
                    .Select(a => a.Name));
                IEnumerable<string> albumArtistsToAdd = file.Tag.AlbumArtists.Except(albumArtistsAlreadyInDatabase);

                IEnumerable<string> artistsAlreadyInDatabase = file.Tag.Performers
                    .Intersect(db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged)
                    .Select(obj => obj.Entity)
                    .OfType<Artist>()
                    .Select(a => a.Name));
                IEnumerable<string> artistsToAdd = file.Tag.Performers
                    .Except(artistsAlreadyInDatabase)
                    .Union(albumArtistsToAdd, StringComparer.Ordinal);
                
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
                    //db.SaveChanges(SaveOptions.None);
                }

                IEnumerable<string> fileAlbumArtistNames = file.Tag.AlbumArtists
                    .Intersect(db.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged)
                    .Select(obj => obj.Entity)
                    .OfType<Artist>()
                    .Select(a => a.Name));

                // Add album if necessary
                // matchingAlbumsInDatabase.Count() should be 0 or 1
                IEnumerable<Album> matchingAlbumsInDatabase = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added
                    | EntityState.Modified | EntityState.Unchanged)
                    .Select(obj => obj.Entity)
                    .OfType<Album>()
                    .Where(album => album.Title.CompareTo(file.Tag.Album) == 0
                        && album.Artists.All(a => fileAlbumArtistNames.Any(s => s.CompareTo(a.Name) == 0))
                        && fileAlbumArtistNames.All(s => album.Artists.Any(a => a.Name.CompareTo(s) == 0)));

                if (matchingAlbumsInDatabase.Count() == 0)
                {
                    Album newAlbum = new Album
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
                        newAlbum.Artists.Add(albumArtist);
                    }

                    db.Albums.AddObject(newAlbum);
                    //Store.SaveChanges(SaveOptions.None);
                }

                // Add track
                Track newTrack = new Track
                {
                    Path = filePath,
                    Title = file.Tag.Title
                };


                IEnumerable<Artist> trackArtists = db.ObjectStateManager.GetObjectStateEntries(EntityState.Added |
                        EntityState.Modified | EntityState.Unchanged)
                        .Select(obj => obj.Entity)
                        .OfType<Artist>()
                        .Where(artist => file.Tag.Performers.Any(s => s.CompareTo(artist.Name) == 0));

                foreach (Artist trackArtist in trackArtists)
                    newTrack.Artists.Add(trackArtist);                    

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
