using System.Collections.Generic;
using System.Configuration;

namespace Howler.Control
{
    class BaseTrackListViewControllerSettings : ApplicationSettingsBase
    {
        private readonly TrackProperty[] _defaultColumnPropertyArray;

        private readonly Dictionary<TrackProperty, int> _defaultColumnWidths;

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

        public BaseTrackListViewControllerSettings(string settingsKey, TrackProperty[] defaultColumnPropertyArray, Dictionary<TrackProperty, int> defaultColumnWidths) : base(settingsKey)
        {
            _defaultColumnPropertyArray = defaultColumnPropertyArray;
            _defaultColumnWidths = defaultColumnWidths;
        }

        public void LoadDefaultColumnPropertyArray()
        {
            ColumnPropertyArray = _defaultColumnPropertyArray;
        }

        public void LoadDefaultColumnWidths()
        {
            ColumnWidths = _defaultColumnWidths;
        }
    }
}