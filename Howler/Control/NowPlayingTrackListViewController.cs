using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gtk;
using Howler.Core.MediaLibrary.Entities;
using Howler.Core.Playback;

namespace Howler.Control
{
    class NowPlayingTrackListViewController : BaseTrackListViewController
    {
        private static readonly TrackProperty[] DefaultColumnPropertyArray = { TrackProperty.Summary, TrackProperty.Duration };

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

        public NowPlayingTrackListViewController(AudioPlayer audioPlayer) :
            base(new TrackListStore(new Track[0]), 
            new BaseTrackListViewControllerSettings("NowPlayingTrackListView", DefaultColumnPropertyArray, DefaultColumnWidths), 
            audioPlayer)
        {
            audioPlayer.PlaylistChanged += AudioPlayerOnPlaylistChanged;
            var trackListStore = TrackListView.Model as TrackListStore;

            Debug.Assert(trackListStore != null, "trackListStore != null");
            trackListStore.DefaultSortFunc = (model, iter1, iter2) =>
                {
                    int index1 = (int) model.GetValue(iter1, 1);
                    int index2 = (int) model.GetValue(iter2, 1);
                    return index1.CompareTo(index2);
                };
        }

        private void AudioPlayerOnPlaylistChanged(object sender, PlaylistChangedEventArgs args)
        {
            var trackListStore = TrackListView.Model as TrackListStore;
            Debug.Assert(trackListStore != null, "trackListStore != null");
            trackListStore.SetNewPlaylist(args.NewPlaylist);
        }

        protected override void TrackListViewOnRowActivated(object o, RowActivatedArgs args)
        {
            int index = args.Path.Indices[0];
            AudioPlayer.PlayTrackAtIndex(index);
        }
    }
}
