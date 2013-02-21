using Iesi.Collections.Generic;

namespace Howler.Core.MediaLibrary.Entities
{
    class Artist
    {
        private ISet<Track> _tracks;
        private ISet<Album> _albums;

        public virtual int Id { get; set; }
        public virtual ISet<Album> Albums
        {
            get { return _albums; }
            set { _albums = value; }
        } 
        public virtual string MusicBrainzId { get; set; }
        public virtual string Name { get; set; }
        public virtual ISet<Track> Tracks
        {
            get { return _tracks; }
            set { _tracks = value; }
        }

        public Artist()
        {
            _albums = new HashedSet<Album>();
            _tracks = new HashedSet<Track>();
        }

        public virtual void AddAlbum(Album album)
        {
            album.Artists.Add(this);
            Albums.Add(album);
        }

        public virtual void AddTrack(Track track)
        {
            track.Artists.Add(this);
            Tracks.Add(track);
        }
    }
}
