using System;
using System.Linq;
using System.Security.Cryptography;
using Iesi.Collections.Generic;

namespace Howler.Core.MediaLibrary.Entities
{
    public class Album
    {
        private ISet<Track> _tracks;
        private ISet<Artist> _artists;
        private string _artistsHash;
 
        public virtual int Id { get; set; }
        public virtual ISet<Artist> Artists
        {
            get { return _artists; }
            set
            {
                _artists = value;
                ArtistsHash = ComputeArtistsHash(_artists);
            }
        }
        public virtual string ArtistsHash
        {
            get { return _artistsHash; }
            set { _artistsHash = value; }
        }
        public virtual int DiscCount { get; set; }
        public virtual string MusicBrainzId { get; set; }
        public virtual string Title { get; set; }
        public virtual ISet<Track> Tracks
        {
            get { return _tracks; }
            set { _tracks = value; }
        } 

        public Album()
        {
            _artists = new HashedSet<Artist>();
            _tracks = new HashedSet<Track>();
            _artistsHash = ComputeArtistsHash(_artists);
        }

        public static string ComputeArtistsHash(ISet<Artist> artists)
        {
            const int sizeOfId = sizeof(int);
            var ids = artists.Select(a => a.Id).OrderBy(id => id).ToArray();
            byte[] byteArray = new byte[ids.Count() * sizeOfId];
            Buffer.BlockCopy(ids, 0, byteArray, 0, ids.Count() * sizeOfId);
            MD5 md5 = new MD5CryptoServiceProvider();
            return Util.Extensions.GetMd5Hash(md5, byteArray);
        }

        protected void AddArtist(Artist artist)
        {
            _artists.Add(artist);
            ArtistsHash = ComputeArtistsHash(_artists);
        }

        public static string ComputeArtistsHash(IQueryable<Artist> artists)
        {
            int sizeOfId = sizeof(int);
            var structLayoutAttribute = typeof(Artist).GetProperty("Id").GetType().StructLayoutAttribute;
            if (structLayoutAttribute != null)
            {
                sizeOfId = structLayoutAttribute.Size;
            }
            var ids = artists.Select(a => a.Id).OrderBy(id => id).ToArray();
            byte[] byteArray = new byte[ids.Count() * sizeOfId];
            Buffer.BlockCopy(ids, 0, byteArray, 0, ids.Count() * sizeOfId);
            MD5 md5 = new MD5CryptoServiceProvider();
            return Util.Extensions.GetMd5Hash(md5, byteArray);
        }
    }
}
