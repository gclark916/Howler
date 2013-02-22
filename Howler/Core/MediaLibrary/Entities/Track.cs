using System;
using System.IO;
using System.Linq;
using Iesi.Collections.Generic;
using TagLib;
using File = TagLib.File;

namespace Howler.Core.MediaLibrary.Entities
{
    public class Track
    {
        private ISet<Artist> _artists;
        private ISet<Genre> _genres; 

        public virtual int Id { get; set; }
        public virtual Album Album { get; set; }
        public virtual ISet<Artist> Artists
        {
            get { return _artists; }
            set { _artists = value; }
        }
        public virtual uint? Bpm { get; set; }
        public virtual uint Bitrate { get; set; }
        public virtual uint BitsPerSample { get; set; }
        public virtual uint ChannelCount { get; set; } 
        public virtual string Codec { get; set; }
        public virtual string Date { get; set; }
        public virtual DateTime DateAdded { get; set; }
        public virtual DateTime? DateLastPlayed { get; set; }
        public virtual uint? DiscNumber { get; set; }

        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        public virtual ulong Duration { get; set; }
        public virtual ISet<Genre> Genres
        {
            get { return _genres; }
            set { _genres = value; }
        }
        public virtual string MusicBrainzId { get; set; }
        public virtual string Path { get; set; }
        public virtual uint Playcount { get; set; }
        public virtual uint? Rating { get; set; }
        public virtual uint SampleRate { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public virtual ulong Size { get; set; }
        public virtual string TagLibHash { get; set; }
        public virtual string Title { get; set; }
        public virtual uint? TrackNumber { get; set; }

        public Track()
        {
            _artists = new HashedSet<Artist>();
            _genres = new HashedSet<Genre>();
        }

        public virtual IPicture GetPicture()
        {
            using (File file = File.Create(Path))
            {
                var picture = file.Tag.Pictures.FirstOrDefault();
                if (picture == null)
                {
                    string[] extensions = { ".jpg", ".jpeg", ".png" };
                    string[] fileNames = {"folder", "cover"};
                    FileInfo info = new FileInfo(Path);
                    if (info.Directory != null)
                    {
                        var files = Directory.EnumerateFiles(info.Directory.FullName, "*", SearchOption.TopDirectoryOnly);
                        var imageFiles = files.Where(s => extensions
                                                      .Any(ext =>
                                                          {
                                                              FileInfo fileInfo = new FileInfo(s);
                                                              return String.Compare(ext, fileInfo.Extension,
                                                                             StringComparison.OrdinalIgnoreCase) == 0;
                                                          }));
                        var imageCoverFiles = imageFiles.Where(s => fileNames.Any(name =>
                            {
                                FileInfo imageInfo = new FileInfo(s);
                                return imageInfo.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase);
                            })).ToArray();

                        if (imageCoverFiles.Any())
                            picture = new Picture(imageCoverFiles.First());
                    }
                }

                return picture;
            }
        }
    }
}
