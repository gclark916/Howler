using Howler.Core.Playback;
using Howler.Gui;

namespace Howler.Control
{
    class PlayerControlPanelController
    {
        private readonly AudioPlayer _audioPlayer;
        public PlayerControlPanel View;

        public PlayerControlPanelController(AudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;

            View = new PlayerControlPanel();
            View.PlayerButtonPanel.StopButton.Clicked += (sender, args) => _audioPlayer.Stop();
            View.PlayerButtonPanel.PreviousTrackButton.Clicked += (sender, args) => _audioPlayer.PreviousTrack();
            View.PlayerButtonPanel.NextTrackButton.Clicked += (sender, args) => _audioPlayer.NextTrack();
            View.PlayerButtonPanel.PlayPauseButton.Clicked += (sender, args) =>
                {
                    if (!_audioPlayer.IsPlaying)
                        _audioPlayer.Play();
                    else
                        _audioPlayer.Pause();
                };
        }
    }
}