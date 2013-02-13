using Gtk;

namespace Howler.Gui
{
    class PlayerControlPanel : HBox
    {
        public VolumeControlWidget VolumeControlWidget;
        public PlayerButtonPanel PlayerButtonPanel;
        public SeekBar SeekBar;
        public TrackRatingWidget TrackRatingWidget;

        public PlayerControlPanel() : base(false, 5)
        {
            VolumeControlWidget = new VolumeControlWidget();
            PlayerButtonPanel = new PlayerButtonPanel();
            SeekBar = new SeekBar();
            TrackRatingWidget = new TrackRatingWidget();

            Add(VolumeControlWidget);
            Add(PlayerButtonPanel);
            Add(SeekBar);
            Add(TrackRatingWidget);
        }
    }

    internal class TrackRatingWidget : Label
    {
    }

    internal class SeekBar : Range
    {
        public SeekBar()
        {
        }
    }

    internal class PlayerButtonPanel : HButtonBox
    {
        public Button StopButton;
        public Button PreviousTrackButton;
        public Button PlayPauseButton;
        public Button NextTrackButton;
        public Button StopAfterCurrentTrackButton;

        public PlayerButtonPanel()
        {
            StopButton = new Button(new Label("Stop"));
            PreviousTrackButton = new Button(new Label("Prev"));
            PlayPauseButton = new Button(new Label("Play"));
            NextTrackButton = new Button(new Label("Next"));
            StopAfterCurrentTrackButton = new Button(new Label("Stop"));

            Add(StopButton);
            Add(PreviousTrackButton);
            Add(PlayPauseButton);
            Add(NextTrackButton);
            Add(StopAfterCurrentTrackButton);
        }
    }

    internal class VolumeControlWidget : Range
    {
    }
}
