using System;
using System.IO;
using System.Data.SQLite;

namespace Howler.Core.Database
{
    class Database
    {
		private SQLiteConnection Connection;
		private String FilePath;

		Database ()
		{
            FilePath = "howler.db";
		}

		public void OpenConnection ()
		{
			if (System.IO.File.Exists (FilePath)) {
				this.Connection = new SQLiteConnection ("URI=file:" + FilePath);
                Connection.Open();
			} else {
				this.Connection = new SQLiteConnection ("URI=file:" + FilePath);
                Connection.Open();
				CreateTables();
			}
			
		}

		public void CloseConnection ()
		{
			Connection.Close();
		}

		private void CreateTables ()
		{
		}
    }
}
