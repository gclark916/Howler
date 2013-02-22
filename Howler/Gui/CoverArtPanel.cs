using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gdk;
using Gtk;
using Howler.Core.MediaLibrary.Entities;
using Howler.Core.Playback;
using TagLib;
using Image = Gtk.Image;
using WindowType = Gtk.WindowType;

namespace Howler.Gui
{
    class CoverArtPanel : Alignment
    {
        private readonly AutoScalingImage _picture = new AutoScalingImage();
        private Track _selectedTrack;
        private Track _playingTrack;
        private readonly ToggleButton _selectedButton;
        private readonly ToggleButton _playingButton;
        private Pixbuf _selectedPixbuf;
        private Pixbuf _playingPixbuf;
        private Pixbuf _currentPixbuf;

        public CoverArtPanel(AudioPlayer audioPlayer, params ITrackSelector[] trackSelectors) : base(0, 0, 1, 1)
        {
            VBox vBox = new VBox(false, 0);
            ButtonBox buttonBox = new HButtonBox();
            _selectedButton = new ToggleButton("Selected");
            _playingButton = new ToggleButton("Playing");
            buttonBox.Add(_playingButton);
            buttonBox.Add(_selectedButton);
            vBox.PackStart(buttonBox, false, false, 0);
            vBox.PackStart(_picture, true, true, 0);
            Add(vBox);

            _selectedButton.Activate();

            _selectedButton.Toggled += SelectedButtonOnToggled;
            _playingButton.Toggled += PlayingButtonOnToggled;

            foreach (var trackSelector in trackSelectors)
            {
                if (trackSelector.HasFocus)
                    _selectedTrack = trackSelector.CurrentTrack;
                trackSelector.SelectedTrack += TrackSelectorOnSelectedTrack;
            }

            audioPlayer.TrackChanged += AudioPlayerOnTrackChanged;

            ShowAll();
        }

        private void PlayingButtonOnToggled(object sender, EventArgs eventArgs)
        {
            if (!_playingButton.Active)
                return;

            if (_selectedButton.Active)
                _selectedButton.Active = false;
            SetPictureAndQueueDraw(_playingTrack, true);
        }

        private void SelectedButtonOnToggled(object sender, EventArgs eventArgs)
        {
            if (!_selectedButton.Active)
                return;

            if (_playingButton.Active)
                _playingButton.Active = false;
            SetPictureAndQueueDraw(_selectedTrack, false);
        }

        private void SetPictureAndQueueDraw(Track track, bool usePlayingTrack)
        {
            if (track == null)
                return;

            IPicture picture = track.GetPicture();
            if (usePlayingTrack)
            {
                _playingPixbuf = _playingPixbuf ?? new Pixbuf(picture.Data.Data);
                _currentPixbuf = _playingPixbuf;
            }
            else
            {
                _selectedPixbuf = _selectedPixbuf ?? new Pixbuf(picture.Data.Data);
                _currentPixbuf = _selectedPixbuf;
            }
            _picture.Pixbuf = _currentPixbuf;
            _picture.QueueDraw();
        }

        private void AudioPlayerOnTrackChanged(object sender, TrackChangedHandlerArgs args)
        {
            _playingTrack = args.NewTrack;
            _playingPixbuf = null;
            if (_playingButton.Active)
                SetPictureAndQueueDraw(_playingTrack, true);
        }

        private void TrackSelectorOnSelectedTrack(object sender, SelectedTrackHandlerArgs args)
        {
            _selectedTrack = args.SelectedTrack;
            _selectedPixbuf = null;
            if (_selectedButton.Active)
                SetPictureAndQueueDraw(_selectedTrack, false);
        }
    }

    internal class AutoScalingImage : Image
    {
        private Pixbuf _originalPixbuf;
        private bool _resize = true;

        public AutoScalingImage()
        {
            AddEvents((int) EventMask.Button1MotionMask);
        }

        protected override void OnSizeAllocated(Rectangle allocation)
        {
            if (_resize)
            {
                int width, height;
                CalculateScaledWidthAndHeight(_originalPixbuf, out width, out height);
                base.Pixbuf = _originalPixbuf.ScaleSimple(width, height, InterpType.Bilinear);
                _resize = false;
            }
            else
            {
                base.OnSizeAllocated(allocation);
                _resize = true;
            }
        }

        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            var window = new Gtk.Window(WindowType.Toplevel);
            var pixbuf = _originalPixbuf.Copy();
            Image image = new Image(pixbuf);
            window.Add(image);
            window.ShowAll();
            return base.OnButtonPressEvent(evnt);
        }

        public new Pixbuf Pixbuf { 
            set 
            { 
                _originalPixbuf = value;
                int width, height;
                CalculateScaledWidthAndHeight(_originalPixbuf, out width, out height);
                base.Pixbuf = _originalPixbuf.ScaleSimple(width, height, InterpType.Bilinear);
            } 
        }

        private void CalculateScaledWidthAndHeight(Pixbuf pixbuf, out int width, out int height)
        {
            int imageWidth = pixbuf.Width;
            int imageHeight = pixbuf.Height;
            int allocWidth = Allocation.Width;
            int allocHeight = Allocation.Height;

            float widthScaleFactor = (float)allocWidth / (float)imageWidth;
            float heightScaleFactor = (float)allocHeight / (float)imageHeight;
            float minScaleFactor = widthScaleFactor < heightScaleFactor ? widthScaleFactor : heightScaleFactor;
            width = (int)(minScaleFactor * (float)imageWidth);
            height = (int)(minScaleFactor * (float)imageHeight);
        }
    }

    public interface ITrackSelector
    {
        Track CurrentTrack { get; }
        bool HasFocus { get; }
        event SelectedTrackHandler SelectedTrack;
    }

    public delegate void SelectedTrackHandler(object sender, SelectedTrackHandlerArgs args);

    public class SelectedTrackHandlerArgs
    {
        public Track SelectedTrack;
    }
}
