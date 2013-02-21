using Iesi.Collections.Generic;

namespace Howler.Core.MediaLibrary.Entities
{
    class Genre
    {
        private ISet<Track> _tracks;

        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual ISet<Track> Tracks
        {
            get { return _tracks; }
            set { _tracks = value; }
        }

        public Genre()
        {
            _tracks = new HashedSet<Track>();
        }

        public virtual void AddTrack(Track track)
        {
            track.Genres.Add(this);
            Tracks.Add(track);
        }
    }
}
