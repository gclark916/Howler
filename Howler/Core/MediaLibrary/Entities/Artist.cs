using System.Collections.Generic;

namespace Howler.Core.MediaLibrary.Entities
{
    class Artist
    {
        private IList<Track> _tracks;
        private IList<Album> _albums;

        public virtual int Id { get; set; }
        public virtual IList<Album> Albums
        {
            get { return _albums; }
            set { _albums = value; }
        } 
        public virtual string MusicBrainzId { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<Track> Tracks
        {
            get { return _tracks; }
            set { _tracks = value; }
        }

        public Artist()
        {
            _albums = new List<Album>();
            _tracks = new List<Track>();
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
