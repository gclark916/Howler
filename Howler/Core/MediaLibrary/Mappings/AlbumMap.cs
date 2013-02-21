using FluentNHibernate.Mapping;
using Howler.Core.MediaLibrary.Entities;

namespace Howler.Core.MediaLibrary.Mappings
{
    public class AlbumMap : ClassMap<Album>
    {
        public AlbumMap()
        {
            Id(album => album.Id);
            HasManyToMany(album => album.Artists)
                .Cascade.SaveUpdate()
                .Table(TableNames.AlbumArtist);
            Map(album => album.ArtistsHash)
                .UniqueKey("UK_ArtistsHash_Title").Not.Nullable();
            Map(album => album.DiscCount);
            Map(album => album.MusicBrainzId);
            Map(album => album.Title)
                .UniqueKey("UK_ArtistsHash_Title").Not.Nullable();
            HasMany(album => album.Tracks)
                .Inverse()
                .Cascade.SaveUpdate();
        }
    }
}
