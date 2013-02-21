using System.Collections.Generic;
using System.Configuration;

namespace Howler.Control
{
    class TracksListViewSettings : ApplicationSettingsBase
    {
        private static readonly TrackProperty[] DefaultColumnPropertyArray = { TrackProperty.TrackNumber, TrackProperty.Title, TrackProperty.Artist, TrackProperty.Album, TrackProperty.AlbumArtist, TrackProperty.Date, TrackProperty.Genre, TrackProperty.Rating, TrackProperty.Bitrate, TrackProperty.Size, TrackProperty.DateAdded, TrackProperty.Codec, TrackProperty.Path };

        private static readonly Dictionary<TrackProperty, int> DefaultColumnWidths = new Dictionary<TrackProperty, int>
            {
                {TrackProperty.Album, 200},
                {TrackProperty.AlbumArtist, 200},
                {TrackProperty.Artist, 200},
                {TrackProperty.Bitrate, 120},
                {TrackProperty.BitsPerSample, 30},
                {TrackProperty.Bpm, 50},
                {TrackProperty.ChannelCount, 20},
                {TrackProperty.Codec, 100},
                {TrackProperty.Date, 200},
                {TrackProperty.DateAdded, 200},
                {TrackProperty.DateLastPlayed, 200},
                {TrackProperty.DiscNumber, 20},
                {TrackProperty.Duration, 50},
                {TrackProperty.Genre, 200},
                {TrackProperty.MusicBrainzAlbumId, 200},
                {TrackProperty.MusicBrainzReleaseId, 200},
                {TrackProperty.Path, 200},
                {TrackProperty.Playcount, 30},
                {TrackProperty.Rating, 200},
                {TrackProperty.SampleRate, 50},
                {TrackProperty.Size, 50},
                {TrackProperty.Summary, 200},
                {TrackProperty.Title, 200},
                {TrackProperty.TrackNumber, 30}
            };

        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public TrackProperty[] ColumnPropertyArray
        {
            get
            {
                return (TrackProperty[])this["ColumnPropertyArray"];
            }
            set
            {
                this["ColumnPropertyArray"] = (TrackProperty[])value;
            }
        }

        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public Dictionary<TrackProperty, int> ColumnWidths
        {
            get
            {
                return (Dictionary<TrackProperty, int>)this["ColumnWidths"];
            }
            set
            {
                this["ColumnWidths"] = (Dictionary<TrackProperty, int>)value;
            }
        }

        public void LoadDefaultColumnPropertyArray()
        {
            ColumnPropertyArray = DefaultColumnPropertyArray;
        }

        public void LoadDefaultColumnWidths()
        {
            ColumnWidths = DefaultColumnWidths;
        }
    }
}