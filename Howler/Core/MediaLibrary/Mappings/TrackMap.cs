using FluentNHibernate.Mapping;
using Howler.Core.MediaLibrary.Entities;

namespace Howler.Core.MediaLibrary.Mappings
{
    class TrackMap : ClassMap<Track>
    {
        public TrackMap()
        {
            Id(track => track.Id);
            References(track => track.Album);
            HasManyToMany(track => track.Artists)
                .Cascade.SaveUpdate()
                .Table(TableNames.TrackArtist);
            Map(track => track.Bitrate);
            Map(track => track.BitsPerSample);
            Map(track => track.Bpm);
            Map(track => track.ChannelCount);
            Map(track => track.Codec);
            Map(track => track.Date);
            Map(track => track.DateAdded);
            Map(track => track.DateLastPlayed);
            Map(track => track.DiscNumber);
            Map(track => track.Duration);
            HasManyToMany(track => track.Genres)
                .Cascade.SaveUpdate()
                .Table(TableNames.TrackGenre);
            Map(track => track.MusicBrainzId);
            Map(track => track.Path)
                .Unique().Not.Nullable();
            Map(track => track.Playcount);
            Map(track => track.Rating);
            Map(track => track.SampleRate);
            Map(track => track.Size);
            Map(track => track.Title);
            Map(track => track.TrackNumber);
        }
    }
}
