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
    class TracksNodeViewController
    {
        private readonly AudioPlayer _audioPlayer;

        public ScrolledWindow View { get; private set;  }
        private readonly TracksListView _tracksListView;
        private readonly ListStore _unfilteredModel;

        delegate string StringPropertySelector(Track track);

        public delegate bool TrackFilter(Track track);

        public TracksNodeViewController(Collection collection)
        {
            _audioPlayer = new AudioPlayer();

            _tracksListView = new TracksListView
                {
                    HeadersClickable = true, 
                    RulesHint = true
                };

            _tracksListView.RowActivated += (o, args) =>
                {
                    TracksListView tracksListView = o as TracksListView;
                    if (tracksListView == null)
                        return;

                    TreeIter iter;
                    tracksListView.Model.GetIter(out iter, args.Path);
                    Track track = (Track) tracksListView.Model.GetValue(iter, 0);
                    _audioPlayer.Stop();
                    _audioPlayer.ClearQueue();
                    string uri = "file:///" + track.Path;
                    _audioPlayer.Enqueue(new[] {uri});
                    _audioPlayer.Play();
                };

            AddTextColumn(t => t.Title, "Title", _tracksListView);
            AddTextColumn(t => t.Artists == null || t.Artists.Count == 0 ? null : String.Join("; ", t.Artists.Select(a => a.Name)), "Artist", _tracksListView);
            AddTextColumn(t => t.Album == null ? null : t.Album.Title, "Album", _tracksListView);
            AddTextColumn(t =>
                {
                    TimeSpan timeSpan = TimeSpan.FromMilliseconds(t.Duration);
                    string length = t.Duration >= 1000*60*60 ? 
                                        String.Format(CultureInfo.CurrentCulture,"{0}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds) 
                                        : String.Format(CultureInfo.CurrentCulture, "{0}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
                    return length;
                }, "Length", _tracksListView);

            IEnumerable<PropertyInfo> stringProperties = typeof(Track).GetProperties()
                .Where(p => p.PropertyType.IsEquivalentTo(typeof(string)));

            foreach (PropertyInfo property in stringProperties)
                AddTextColumnUsingStringProperty(property, property.Name, _tracksListView);

            _unfilteredModel = new ListStore(typeof(Track));

            foreach (Track track in collection.GetTracks())
            {
                _unfilteredModel.AppendValues(track);
            }

            _tracksListView.Model = _unfilteredModel;

            View = new ScrolledWindow {_tracksListView};
        }

        public void FilterStore(TrackFilter trackFilter)
        {
            TreeModelFilter filter = new TreeModelFilter(_unfilteredModel, null)
                {
                    VisibleFunc = delegate(TreeModel model, TreeIter iter)
                        {
                            Track track = (Track) model.GetValue(iter, 0);
                            return trackFilter(track);
                        }
                };

            _tracksListView.Model = filter;
        }

        private void AddTextColumn(StringPropertySelector selector, string columnName, TreeView treeView)
        {
            TracksListViewColumn genericColumn = new TracksListViewColumn(columnName);

            TracksCellRenderer pathCellTextRenderer = new TracksCellRenderer();
            genericColumn.PackStart(pathCellTextRenderer, true);
            genericColumn.SetCellDataFunc(pathCellTextRenderer,
                (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) =>
                {
                    Track track = (Track)model.GetValue(iter, 0);
                    ((CellRendererText) cell).Text = selector.Invoke(track);
                });

            treeView.AppendColumn(genericColumn);
        }

        private void AddTextColumnUsingStringProperty(PropertyInfo property, string columnName, TreeView treeView)
        {
            TracksListViewColumn genericColumn = new TracksListViewColumn(columnName);

            TracksCellRenderer pathCellTextRenderer = new TracksCellRenderer();
            genericColumn.PackStart(pathCellTextRenderer, true);
            genericColumn.SetCellDataFunc(pathCellTextRenderer,
                (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) =>
                {
                    Track track = (Track)model.GetValue(iter, 0);
                    ((CellRendererText) cell).Text = (string)property.GetGetMethod().Invoke(track, null);
                });

            treeView.AppendColumn(genericColumn);
        }
    }
}
