using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;

namespace Howler.Control
{
    internal class BaseTrackListViewControllerSettings : ApplicationSettingsBase
    {
        private readonly List<TrackProperty> _defaultColumnPropertyList;

        private readonly Dictionary<TrackProperty, int> _defaultColumnWidths;

        private readonly Timer _saveTimer;

        private const int SaveTimerResetTime = 1000;

        public BaseTrackListViewControllerSettings(string settingsKey, IEnumerable<TrackProperty> defaultColumnPropertyArray,
                                                   Dictionary<TrackProperty, int> defaultColumnWidths)
            : base(settingsKey)
        {
            _defaultColumnPropertyList = defaultColumnPropertyArray.ToList();
            _defaultColumnWidths = defaultColumnWidths;
            _saveTimer = new Timer(state => Save(), null, Timeout.Infinite, Timeout.Infinite);
        }

        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public List<TrackProperty> ColumnPropertyList
        {
            get { return (List<TrackProperty>)this["ColumnPropertyList"]; }
            set
            {
                this["ColumnPropertyList"] = value;
                _saveTimer.Change(SaveTimerResetTime, Timeout.Infinite);
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
                _saveTimer.Change(SaveTimerResetTime, Timeout.Infinite);
            }
        }

        public void LoadDefaultColumnPropertyArray()
        {
            ColumnPropertyList = _defaultColumnPropertyList;
        }

        public void LoadDefaultColumnWidths()
        {
            ColumnWidths = _defaultColumnWidths;
        }

        public void AddColumn(TrackProperty propertyToInsert)
        {
            ColumnPropertyList.Add(propertyToInsert);
            _saveTimer.Change(SaveTimerResetTime, Timeout.Infinite);
        }

        public void RemoveColumn(TrackProperty property)
        {
            ColumnPropertyList.Remove(property);
            _saveTimer.Change(SaveTimerResetTime, Timeout.Infinite);
        }

        public void InsertColumn(TrackProperty propertyToInsert, TrackProperty? priorColumnProperty)
        {
            int position = 0;
            foreach (var property in ColumnPropertyList)
            {
                position++;
                if (property == priorColumnProperty)
                    break;
            }

            ColumnPropertyList.Insert(position, propertyToInsert);
            _saveTimer.Change(SaveTimerResetTime, Timeout.Infinite);
        }
    }
}