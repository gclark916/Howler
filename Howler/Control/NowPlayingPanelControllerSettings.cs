using System.Configuration;
using System.Threading;

namespace Howler.Control
{
    class NowPlayingPanelControllerSettings : ApplicationSettingsBase
    {
        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public int VPanedPosition
        {
            get { return (int)(this["VPanedPosition"] ?? 500); }
            set
            {
                this["VPanedPosition"] = value;
                _saveTimer.Change(1000, Timeout.Infinite);
            }
        }

        private readonly Timer _saveTimer;

        public NowPlayingPanelControllerSettings()
        {
            _saveTimer = new Timer(state => Save(), null, Timeout.Infinite, Timeout.Infinite);
        }
    }
}