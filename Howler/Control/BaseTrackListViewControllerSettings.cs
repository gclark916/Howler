using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace Howler.Control
{
    internal class BaseTrackListViewControllerSettings : ApplicationSettingsBase
    {
        private readonly TrackProperty[] _defaultColumnPropertyArray;

        private readonly Dictionary<TrackProperty, int> _defaultColumnWidths;

        private readonly Timer _saveTimer;

        public BaseTrackListViewControllerSettings(string settingsKey, TrackProperty[] defaultColumnPropertyArray,
                                                   Dictionary<TrackProperty, int> defaultColumnWidths)
            : base(settingsKey)
        {
            _defaultColumnPropertyArray = defaultColumnPropertyArray;
            _defaultColumnWidths = defaultColumnWidths;
            _saveTimer = new Timer(state => Save(), null, Timeout.Infinite, Timeout.Infinite);
        }

        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public TrackProperty[] ColumnPropertyArray
        {
            get { return (TrackProperty[]) this["ColumnPropertyArray"]; }
            set
            {
                this["ColumnPropertyArray"] = value;
                _saveTimer.Change(1000, Timeout.Infinite);
            }
        }

        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public Dictionary<TrackProperty, int> ColumnWidths
        {
            get { return (Dictionary<TrackProperty, int>) this["ColumnWidths"]; }
            set
            {
                this["ColumnWidths"] = value;
                _saveTimer.Change(1000, Timeout.Infinite);
            }
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