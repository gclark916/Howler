using FluentNHibernate.Mapping;
using Howler.Core.MediaLibrary.Entities;

namespace Howler.Core.MediaLibrary.Mappings
{
    public class ArtistMap : ClassMap<Artist>
    {
        public ArtistMap()
        {
            Id(artist => artist.Id)
                .GeneratedBy.HiLo("1000");
            HasManyToMany(artist => artist.Albums)
                .Inverse()
                .Cascade.SaveUpdate()
                .Table(TableNames.AlbumArtist);
            Map(artist => artist.MusicBrainzId);
            Map(artist => artist.Name)
                .Unique().Not.Nullable();
            HasManyToMany(artist => artist.Tracks)
                .Inverse()
                .Cascade.SaveUpdate()
                .Table(TableNames.TrackArtist);
        }
    }
}