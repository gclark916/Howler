using System;
using System.IO;

namespace Howler
{
	public class LocalTrack
	{
		System.IO.Path FilePath { get; set; }

		public LocalTrack (Path filePath)
		{
			this.FilePath = filePath;
		}
	}
}

