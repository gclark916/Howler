using System.Collections.Generic;
using System.Diagnostics;
using Gtk;
using Howler.Core.MediaLibrary;
using Howler.Core.MediaLibrary.Entities;
using Howler.Core.Playback;
using Howler.Gui;

namespace Howler.Control
{
    class FilteredTrackListViewController : BaseTrackListViewController
    {
        private static readonly TrackProperty[] DefaultColumnPropertyArray =
            {
                TrackProperty.TrackNumber, 
                TrackProperty.Title, 
                TrackProperty.Artist, 
                TrackProperty.Album, 
                TrackProperty.AlbumArtist, 
                TrackProperty.Date, 
                TrackProperty.Genre, 
                TrackProperty.Rating, 
                TrackProperty.Bitrate, 
                TrackProperty.Size, 
                TrackProperty.DateAdded, 
                TrackProperty.Codec, 
                TrackProperty.Path
            };

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

        public FilteredTrackListViewController(Collection collection, AudioPlayer audioPlayer) : 
            base(new FilteredTrackListModel(collection), 
            new BaseTrackListViewControllerSettings("FilteredTrackListView", DefaultColumnPropertyArray, DefaultColumnWidths), 
            audioPlayer)
        {
            TrackListViewColumn column;
            if (PropertiesToColumns.TryGetValue(TrackProperty.Artist, out column))
                ((TreeModelSort) TrackListView.Model).SetSortColumnId(column.SortColumnId, SortType.Ascending);
        }

        protected override void TrackListViewOnRowActivated(object o, RowActivatedArgs args)
        {
            TrackListView trackListView = o as TrackListView;
            if (trackListView == null)
                return;

            int numTracks = trackListView.Model.IterNChildren();
            Track[] trackArray = new Track[numTracks];
            TreeIter iter;
            int selectedTrackIndex = args.Path.Indices[0];
            trackListView.Model.GetIterFirst(out iter);

            uint trackIndex = 0;
            bool valid = trackListView.Model.GetIterFirst(out iter);
            while (valid)
            {
                Track track = (Track)trackListView.Model.GetValue(iter, 0);
                trackArray[trackIndex++] = track;
                valid = trackListView.Model.IterNext(ref iter);
            }
            AudioPlayer.ReplacePlaylistAndPlay(trackArray, (uint)selectedTrackIndex);
        }

        public void FilterStore(FilteredTrackListModel.TrackFilter trackFilter)
        {
            var trackListModel = TrackListView.Model as FilteredTrackListModel;
            Debug.Assert(trackListModel != null, "trackListModel != null");

            trackListModel.Filter = trackFilter;
            TrackListView.Selection.SelectPath(new TreePath(new int[1] {0}));
        }
    }
}
