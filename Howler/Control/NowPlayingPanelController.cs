using Gtk;
using Howler.Core.Playback;
using Howler.Gui;

namespace Howler.Control
{
    class NowPlayingPanelController
    {
        private readonly NowPlayingTrackListViewController _nowPlayingTrackListViewController;
        private readonly CoverArtPanel _coverArtPanel;
        private readonly NowPlayingPanelControllerSettings _settings = new NowPlayingPanelControllerSettings();

        public NowPlayingPanelController(AudioPlayer audioPlayer, ITrackSelector trackSelector)
        {
            if (_settings.VPanedPosition == 0)
                _settings.VPanedPosition = 500;

            _nowPlayingTrackListViewController = new NowPlayingTrackListViewController(audioPlayer);
            _coverArtPanel = new CoverArtPanel(audioPlayer, trackSelector, _nowPlayingTrackListViewController);

            View = new VPaned();
            View.Pack1(_nowPlayingTrackListViewController.View, true, true);
            View.Pack2(_coverArtPanel, true, true);

            View.Position = _settings.VPanedPosition;
            View.PositionSet = true;

            View.AddNotification("position", (o, args) =>
                {
                    _settings.VPanedPosition = View.Position;
                });
        }

        public VPaned View { get; private set; }
    }
}
