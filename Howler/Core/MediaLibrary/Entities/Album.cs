using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Howler.Core.MediaLibrary.Entities
{
    class Album
    {
        private IList<Track> _tracks;
        private IList<Artist> _artists;
 
        public virtual int Id { get; set; }
        public virtual IList<Artist> Artists
        {
            get { return _artists; }
            set
            {
                _artists = value;

                ComputeNewArtistsHash();
            }
        }
        public virtual string ArtistsHash { get; protected set; }
        public virtual int DiscCount { get; set; }
        public virtual string MusicBrainzId { get; set; }
        public virtual string Title { get; set; }
        public virtual IList<Track> Tracks
        {
            get { return _tracks; }
            set { _tracks = value; }
        } 

        public Album()
        {
            _artists = new List<Artist>();
            _tracks = new List<Track>();
            ComputeNewArtistsHash();
        }

        protected void ComputeNewArtistsHash()
        {
            int sizeOfId = sizeof(int);
            var structLayoutAttribute = typeof(Artist).GetProperty("Id").GetType().StructLayoutAttribute;
            if (structLayoutAttribute != null)
            {
                sizeOfId = structLayoutAttribute.Size;
            }
            var ids = _artists.Select(a => a.Id).OrderBy(id => id).ToArray();
            byte[] byteArray = new byte[ids.Count() * sizeOfId];
            Buffer.BlockCopy(ids, 0, byteArray, 0, ids.Count() * sizeOfId);
            MD5 md5 = new MD5CryptoServiceProvider();
            ArtistsHash = Util.Extensions.GetMd5Hash(md5, byteArray);
        }

        protected void AddArtist(Artist artist)
        {
            _artists.Add(artist);
            ComputeNewArtistsHash();
        }
    }
}
