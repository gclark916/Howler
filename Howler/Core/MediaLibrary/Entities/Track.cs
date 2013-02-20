using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Howler.Core.MediaLibrary.Entities
{
    class Track
    {
        private IList<Artist> _artists;
        private IList<Genre> _genres; 

        public virtual int Id { get; set; }
        public virtual Album Album { get; set; }
        public virtual IList<Artist> Artists
        {
            get { return _artists; }
            set { _artists = value; }
        }
        public virtual int Bpm { get; set; }
        public virtual int Bitrate { get; set; }
        public virtual int BitsPerSample { get; set; }
        public virtual int ChannelCount { get; set; } 
        public virtual string Codec { get; set; }
        public virtual string Date { get; set; }
        public virtual DateTime DateAdded { get; set; }
        public virtual DateTime DateLastPlayed { get; set; }
        public virtual int DiscNumber { get; set; }
        public virtual int Duration { get; set; }
        public virtual IList<Genre> Genres
        {
            get { return _genres; }
            set { _genres = value; }
        }
        public virtual string MusicBrainzId { get; set; }
        public virtual string Path { get; set; }
        public virtual int Playcount { get; set; }
        public virtual int Rating { get; set; }
        public virtual int SampleRate { get; set; }
        public virtual int Size { get; set; }
        public virtual string Title { get; set; }
        public virtual int TrackNumber { get; set; }

        public Track()
        {
            _artists = new List<Artist>();
            _genres = new List<Genre>();
        }
    }
}
