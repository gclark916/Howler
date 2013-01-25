using System;
using System.IO;

namespace Howler
{
	public class LocalTrack
	{
		string Path { get; set; }

		public LocalTrack (string path)
		{
			this.Path = path;
		}
	}
}

