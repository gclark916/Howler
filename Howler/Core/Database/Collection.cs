using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
using System.Data.Objects;
using System.Transactions;
using System.Data.SQLite;
using Howler.Core.Database;

namespace Howler.Core.Database
{
    class Collection
    {
        CollectionContainer Store;

        public Collection()
        {
            string fileName = "howler.db";
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

            Store = new CollectionContainer();
        }

        public void ImportDirectory(String path)
        {
            if (Directory.Exists(path))
            {
                IEnumerable<string> newFiles = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);

                using (TransactionScope scope = new TransactionScope())
                {
                    Store.Connection.Open();

                    foreach (string file in newFiles)
                    {
                        ImportFile(file);
                    }

                    try
                    {
                        Store.SaveChanges(SaveOptions.None);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        scope.Complete();
                        Store.AcceptAllChanges();
                    }
                }
            }
        }

        private void ImportFile(string file)
        {
            IEnumerable<Track> tracks = from Track in Store.Tracks
                                        where Track.Path.CompareTo(file) == 0
                                        select Track;

            if (tracks == null)
            {
                // Update metadata
            }
            else
            {
                Track track = new Track
                {
                    Path = file
                };

                Store.Tracks.AddObject(track);
            }

        }
    }
}
