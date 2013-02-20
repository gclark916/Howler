using System.Collections.Generic;

namespace Howler.Core.MediaLibrary.Entities
{
    class Genre
    {
        private IList<Track> _tracks;

        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<Track> Tracks
        {
            get { return _tracks; }
            set { _tracks = value; }
        }

        public Genre()
        {
            _tracks = new List<Track>();
        }

        public virtual void AddTrack(Track track)
        {
            track.Genres.Add(this);
            Tracks.Add(track);
        }
    }
}
