﻿using System;
using Iesi.Collections.Generic;

namespace Howler.Core.MediaLibrary.Entities
{
    class Track
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
    }
}
