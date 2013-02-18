using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Gtk;
using Howler.Core.Playback;
using Howler.Gui;
using Howler.Core.Database;

namespace Howler.Control
{
    class TracksListViewController
    {
        private readonly AudioPlayer _audioPlayer;

        public ScrolledWindow View { get; private set;  }
        private readonly TracksListView _tracksListView;
        private readonly ListStore _unfilteredModel;
        private readonly TreeModelFilter _filteredModel;
        private TrackFilter _trackFilter;
        private readonly Dictionary<Track, TreeIter> _unfilteredTrackIters;

        delegate string StringPropertySelector(Track track);

        public delegate bool TrackFilter(Track track);

        public TracksListViewController(Collection collection, AudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;

            _tracksListView = new TracksListView
                {
                    HeadersClickable = true, 
                    RulesHint = true
                };

            _tracksListView.RowActivated += TracksListViewOnRowActivated;

            _unfilteredModel = new ListStore(typeof (Track));

            _unfilteredTrackIters = new Dictionary<Track, TreeIter>();

            foreach (Track track in collection.GetTracks())
            {
                TreeIter trackIter = _unfilteredModel.AppendValues(track);
                _unfilteredTrackIters.Add(track, trackIter);
            }

            _trackFilter = track => true;
            _filteredModel = new TreeModelFilter(_unfilteredModel, null)
                {
                    VisibleFunc = (model, iter) =>
                        {
                            Track track = (Track) model.GetValue(iter, 0);
                            return _trackFilter(track);
                        }
                };

            _tracksListView.Model = new TrackListModel(_filteredModel);

            int sortColumnId = 0;
            AddTextColumn(t => t.Title, "Title", sortColumnId++);
            AddTextColumn(t => t.Artists == null || t.Artists.Count == 0 ? null : String.Join("; ", t.Artists.Select(a => a.Name)), "Artist", sortColumnId++);
            AddTextColumn(t => 
                          (t.Album == null || t.Album.Artists == null || t.Artists.Count == 0)  ? null :
                              String.Join("; ", t.Album.Artists.Select(a => a.Name)), "Album Artist", sortColumnId++);
            AddTextColumn(t => t.Album == null ? null : t.Album.Title, "Album", sortColumnId++);
            AddTextColumn(t =>
                {
                    TimeSpan timeSpan = TimeSpan.FromMilliseconds(t.Duration);
                    string length = t.Duration >= 1000*60*60 ? 
                                        String.Format(CultureInfo.CurrentCulture,"{0}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds) 
                                        : String.Format(CultureInfo.CurrentCulture, "{0}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
                    return length;
                }, "Length", -1);

            IEnumerable<PropertyInfo> stringProperties = typeof(Track).GetProperties()
                .Where(p => p.PropertyType.IsEquivalentTo(typeof(string)));
            foreach (PropertyInfo property in stringProperties)
                AddTextColumnUsingStringProperty(property, property.Name, sortColumnId++);

            _audioPlayer.TrackChanged += AudioPlayerOnTrackChanged;

            ((TreeModelSort)_tracksListView.Model).DefaultSortFunc = DefaultSortFunc;
            ((TreeModelSort)_tracksListView.Model).SetSortColumnId(1, SortType.Ascending);

            View = new ScrolledWindow {_tracksListView};
        }

        private static int DefaultSortFunc(TreeModel model, TreeIter iter1, TreeIter iter2)
        {
            //TODO: add discnumber comparison
            Track track1 = (Track)model.GetValue(iter1, 0);
            Track track2 = (Track)model.GetValue(iter2, 0);
            int result = string.Compare(string.Join("", track1.Artists), string.Join("", track2.Artists), StringComparison.CurrentCulture);
            if (result == 0)
                result = string.Compare(string.Join("", track1.Album.Artists), string.Join("", track2.Album.Artists),
                                        StringComparison.CurrentCulture);
            if (result == 0)
                result = string.Compare(string.Join("", track1.Album.Title), string.Join("", track2.Album.Title),
                                        StringComparison.CurrentCulture);
            if (result == 0)
            {
                Int64 trackNumber1 = track1.TrackNumber ?? Int64.MinValue;
                Int64 trackNumber2 = track1.TrackNumber ?? Int64.MinValue;
                result = trackNumber1.CompareTo(trackNumber2);
            }

            return result;
        }

        private void TracksListViewOnRowActivated(object o, RowActivatedArgs args)
        {
            TracksListView tracksListView = o as TracksListView;
            if (tracksListView == null)
                return;

            int numTracks = tracksListView.Model.IterNChildren();
            Track[] trackArray = new Track[numTracks];
            TreeIter iter;
            int selectedTrackIndex = args.Path.Indices[0];
            tracksListView.Model.GetIterFirst(out iter);

            uint trackIndex = 0;
            bool valid = tracksListView.Model.GetIterFirst(out iter);
            while (valid)
            {
                Track track = (Track) tracksListView.Model.GetValue(iter, 0);
                trackArray[trackIndex++] = track;
                valid = tracksListView.Model.IterNext(ref iter);
            }
            _audioPlayer.ReplacePlaylistAndPlay(trackArray, (uint)selectedTrackIndex);
        }

        private void AudioPlayerOnTrackChanged(object sender, TrackChangedHandlerArgs args)
        {
            TreeIter iter = new TreeIter();
            ((TrackListModel) _tracksListView.Model).CurrentTrack = args.NewTrack;
            bool exists = args.NewTrack != null && _unfilteredTrackIters.TryGetValue(args.NewTrack, out iter);
            if (exists)
            {
                TreeIter filteredIter = _filteredModel.ConvertChildIterToIter(iter);
                TreeIter newIter = ((TreeModelSort)_tracksListView.Model).ConvertChildIterToIter(filteredIter);
                ((TreeModelSort)_tracksListView.Model).EmitRowChanged(_tracksListView.Model.GetPath(newIter), newIter);
            }

            exists = args.OldTrack != null && _unfilteredTrackIters.TryGetValue(args.OldTrack, out iter);
            if (exists)
            {
                TreeIter filteredIter = _filteredModel.ConvertChildIterToIter(iter);
                TreeIter oldIter = ((TreeModelSort) _tracksListView.Model).ConvertChildIterToIter(filteredIter);
                ((TreeModelSort) _tracksListView.Model).EmitRowChanged(_tracksListView.Model.GetPath(oldIter), oldIter);
            }
        }

        public void FilterStore(TrackFilter trackFilter)
        {
            _trackFilter = trackFilter;
            _filteredModel.Refilter();
        }

        private void AddTextColumn(StringPropertySelector selector, string columnName, int sortColumnId)
        {
            TracksListViewColumn genericColumn = new TracksListViewColumn(columnName);

            TracksCellRenderer pathCellTextRenderer = new TracksCellRenderer();
            genericColumn.PackStart(pathCellTextRenderer, true);
            genericColumn.SetCellDataFunc(pathCellTextRenderer,
                (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) =>
                    {
                        Track track = (Track)model.GetValue(iter, 0);
                        Track playingTrack = ((TrackListModel) model).CurrentTrack;
                        bool playing = playingTrack != null && track.Equals(playingTrack);
                        ((CellRendererText) cell).Text = selector(track);
                        ((CellRendererText) cell).Weight = playing ? 800 : 400;
                    });

            if (sortColumnId >= 0)
            {
                genericColumn.SortColumnId = sortColumnId;
                ((TreeModelSort)_tracksListView.Model).SetSortFunc(sortColumnId, (model, iter1, iter2) =>
                    {
                        Track track1 = (Track) model.GetValue(iter1, 0);
                        Track track2 = (Track) model.GetValue(iter2, 0);
                        int result = string.Compare(selector(track1), selector(track2), StringComparison.CurrentCulture);
                        if (result == 0)
                            result = DefaultSortFunc(model, iter1, iter2);
                        return result;
                    });
            }

            _tracksListView.AppendColumn(genericColumn);
        }

        private void AddTextColumnUsingStringProperty(PropertyInfo property, string columnName, int sortColumnId)
        {
            TracksListViewColumn genericColumn = new TracksListViewColumn(columnName);

            TracksCellRenderer pathCellTextRenderer = new TracksCellRenderer();
            genericColumn.PackStart(pathCellTextRenderer, true);
            genericColumn.SetCellDataFunc(pathCellTextRenderer,
                (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) =>
                {
                    Track track = (Track)model.GetValue(iter, 0);
                    Track playingTrack = ((TrackListModel)model).CurrentTrack;
                    bool playing = playingTrack != null && track.Equals(playingTrack);
                    ((CellRendererText)cell).Text = (string)property.GetGetMethod().Invoke(track, null);
                    ((CellRendererText)cell).Weight = playing ? 800 : 400;
                });

            genericColumn.SortColumnId = sortColumnId;
            ((TreeModelSort)_tracksListView.Model).SetSortFunc(sortColumnId, (model, iter1, iter2) =>
                {
                    Track track1 = (Track) model.GetValue(iter1, 0);
                    Track track2 = (Track) model.GetValue(iter2, 0);
                    int result = string.Compare((string) property.GetGetMethod().Invoke(track1, null),
                                                (string) property.GetGetMethod().Invoke(track2, null),
                                                StringComparison.CurrentCulture);
                    if (result == 0)
                        result = DefaultSortFunc(model, iter1, iter2);
                    return result;
                });

            _tracksListView.AppendColumn(genericColumn);
        }

        class TrackListModel : TreeModelSort 
        {
            public TrackListModel(TreeModel childModel) : base(childModel)
            {
                CurrentTrack = null;
            }

            public Track CurrentTrack { get; set; }
        }
    }
}
