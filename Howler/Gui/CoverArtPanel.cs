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
    class CoverArtPanel : VBox
    {
        private readonly AutoScalingImage _picture = new AutoScalingImage();
        private Track _selectedTrack;
        private Track _playingTrack;
        private readonly ToggleButton _selectedButton;
        private readonly ToggleButton _playingButton;
        private Pixbuf _selectedPixbuf;
        private Pixbuf _playingPixbuf;
        private Pixbuf _currentPixbuf;

        public CoverArtPanel(AudioPlayer audioPlayer, params ITrackSelector[] trackSelectors) : base(false, 0)
        {
            ButtonBox buttonBox = new HButtonBox();
            _selectedButton = new ToggleButton("Selected");
            _playingButton = new ToggleButton("Playing");
            buttonBox.Add(_playingButton);
            buttonBox.Add(_selectedButton);
            PackStart(buttonBox, false, false, 0);

            EventBox eventBox = new EventBox();
            eventBox.Add(_picture);
            eventBox.ButtonPressEvent += (o, args) =>
                {
                    var window = new Gtk.Window(WindowType.Toplevel);
                    var pixbuf = _currentPixbuf.Copy();
                    AutoScalingImage image = new AutoScalingImage();
                    window.Add(image);
                    int left, top, right, bottom;
                    window.GetFrameDimensions(out left, out top, out right, out bottom);
                    int windowHeightWithFullImage = pixbuf.Height + top + bottom;
                    if (windowHeightWithFullImage > Screen.Height)
                        window.SetDefaultSize(((pixbuf.Width + left + right) * Screen.Height) / windowHeightWithFullImage, Screen.Height);
                    else
                        window.SetDefaultSize(pixbuf.Width + left + right, pixbuf.Height + top + bottom);
                    window.AllowShrink = true;
                    image.Pixbuf = pixbuf;

                    Track track = _selectedButton.Active ? _selectedTrack : _playingTrack;
                    window.Title = String.Format("{0} - {1}",
                        string.Join("; ", track.Album.Artists.Select(a => a.Name)),
                        track.Album.Title);
                    window.ShowAll();
                    image.QueueResize();

                    window.SizeAllocated += (o1, allocatedArgs) =>
                        {
                            Console.WriteLine("window: {0}", window.Allocation);
                            Console.WriteLine("args: {0}", allocatedArgs.Allocation);
                            if (window.Allocation.Width != allocatedArgs.Allocation.Width ||
                                window.Allocation.Height != allocatedArgs.Allocation.Height)
                            {
                                image.SizeAllocate(allocatedArgs.Allocation);
                                image.QueueResize();
                                image.QueueDraw();
                                window.QueueDraw();
                            }
                        };
                };

            PackStart(eventBox, true, true, 0);

            _selectedButton.Toggled += SelectedButtonOnToggled;
            _playingButton.Toggled += PlayingButtonOnToggled;

            foreach (var trackSelector in trackSelectors)
            {
                if (trackSelector.HasFocus)
                    _selectedTrack = trackSelector.CurrentTrack;
                trackSelector.SelectedTrack += TrackSelectorOnSelectedTrack;
            }

            audioPlayer.TrackChanged += AudioPlayerOnTrackChanged;

            _selectedButton.Shown += (sender, args) => _selectedButton.Active = true;

            ShowAll();
        }

        private void PlayingButtonOnToggled(object sender, EventArgs eventArgs)
        {
            if (!_playingButton.Active)
            {
                if (!_selectedButton.Active)
                    _selectedButton.Active = true;
                return;
            }

            if (_selectedButton.Active)
                _selectedButton.Active = false;
            SetPictureAndQueueDraw(_playingTrack, true);
        }

        private void SelectedButtonOnToggled(object sender, EventArgs eventArgs)
        {
            if (!_selectedButton.Active)
            {
                if (!_playingButton.Active)
                    _playingButton.Active = true;
                return;
            }

            if (_playingButton.Active)
                _playingButton.Active = false;
            SetPictureAndQueueDraw(_selectedTrack, false);
        }

        private void SetPictureAndQueueDraw(Track track, bool usePlayingTrack)
        {
            if (track == null)
                return;

            IPicture picture = track.GetPicture() ?? new Picture("Images/DefaultAlbumArt.png");

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

        private void AudioPlayerOnTrackChanged(object sender, TrackChangedEventArgs args)
        {
            _playingTrack = args.NewTrack;
            _playingPixbuf = null;
            if (_playingButton.Active)
                SetPictureAndQueueDraw(_playingTrack, true);
        }

        private void TrackSelectorOnSelectedTrack(object sender, SelectedTrackEventArgs args)
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

        protected override void OnSizeAllocated(Rectangle allocation)
        {
            Console.WriteLine("image allocate: {0} {1}", allocation, _resize);
            if (_originalPixbuf != null && _resize)
            {
                int width, height;
                CalculateScaledWidthAndHeight(_originalPixbuf, out width, out height);
                base.Pixbuf = _originalPixbuf.ScaleSimple(width, height, InterpType.Bilinear);
                _resize = false;
                QueueResize();
                QueueDraw();
            }
            else
            {
                base.OnSizeAllocated(allocation);
                _resize = true;
            }
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
            width = (int) Math.Ceiling(minScaleFactor * (float)imageWidth);
            height = (int) Math.Ceiling(minScaleFactor * (float)imageHeight);

            if (width < 100 || height < 100)
            {
                width = 100;
                height = 100;
            }
        }
    }

    public interface ITrackSelector
    {
        Track CurrentTrack { get; }
        bool HasFocus { get; }
        event SelectedTrackHandler SelectedTrack;
    }

    public delegate void SelectedTrackHandler(object sender, SelectedTrackEventArgs args);

    public class SelectedTrackEventArgs : EventArgs
    {
        public Track SelectedTrack;
    }
}
