using System;
using System.Linq;
using Howler.Core.MediaLibrary;
using Howler.Core.MediaLibrary.Entities;
using Howler.Gui;
using Gtk;

namespace Howler.Control
{
    class SourceTreeViewController
    {
        public ScrolledWindow View { get; private set; }
        private readonly SourceTreeView _sourceTreeView;
        private readonly FilteredTrackListViewController _filteredTrackListViewController;

        public SourceTreeViewController(FilteredTrackListViewController filteredTrackListViewController, Collection collection)
        {
            _filteredTrackListViewController = filteredTrackListViewController;

            _sourceTreeView = new SourceTreeView();
            _sourceTreeView.Model = CreateSourceTreeStore(collection);

            SourceTreeViewColumn sourceColumn = new SourceTreeViewColumn();
            SourceCellRenderer sourceCellRenderer = new SourceCellRenderer();
            sourceColumn.PackStart(sourceCellRenderer, true);
            sourceColumn.SetCellDataFunc(sourceCellRenderer,
                (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) =>
                {
                    SourceTreeStoreValue value = (SourceTreeStoreValue)model.GetValue(iter, 0);
                    ((CellRendererText)cell).Text = value.DisplayString;
                });
            _sourceTreeView.AppendColumn(sourceColumn);

            _sourceTreeView.Selection.Changed += SelectionOnChanged;

            View = new ScrolledWindow {_sourceTreeView};
            View.SetSizeRequest(200, -1);
        }

        private void SelectionOnChanged(object sender, EventArgs eventArgs)
        {
            var treeSelection = sender as TreeSelection;
            if (treeSelection == null)
                return;

            TreeModel model;
            TreeIter iter;
            treeSelection.GetSelected(out model, out iter);
            SourceTreeStoreValue value = (SourceTreeStoreValue)model.GetValue(iter, 0);
            if (value != null)
                _filteredTrackListViewController.FilterStore(value.TrackFilter);
        }

        private static TreeStore CreateSourceTreeStore(Collection collection)
        {
            TreeStore store = new TreeStore(typeof(SourceTreeStoreValue));

            SourceTreeStoreValue musicRow = new SourceTreeStoreValue
            {
                DisplayString = "Music",
                TrackFilter = track => true
            };
            TreeIter musicIter = store.AppendValues(musicRow);

            SourceTreeStoreValue artistAndAlbumArtistRow = new SourceTreeStoreValue
            {
                DisplayString = "Artist & Album Artist",
                TrackFilter = track => true
            };
            TreeIter artistAndAlbumArtistIter = store.AppendValues(musicIter, artistAndAlbumArtistRow);

            foreach (Artist artist in collection.GetTrackArtistsAndAlbumArtists())
            {
                Artist someArtist = artist;
                SourceTreeStoreValue someArtistRow = new SourceTreeStoreValue
                {
                    DisplayString = artist.Name,
                    TrackFilter = track => track.Album.Artists.Any(a => a.Id == someArtist.Id) || track.Artists.Any(a => a.Id == someArtist.Id)
                };
                TreeIter someArtistIter = store.AppendValues(artistAndAlbumArtistIter, someArtistRow);

                foreach (Album album in someArtist.Albums)
                {
                    Album someAlbum = album;
                    SourceTreeStoreValue someAlbumRow = new SourceTreeStoreValue
                    {
                        DisplayString = album.Title,
                        TrackFilter =
                            track => string.Compare(track.Album.Title, someAlbum.Title, StringComparison.Ordinal) == 0
                    };
                    store.AppendValues(someArtistIter, someAlbumRow);
                }
            }

            return store;
        }
    }

    class SourceTreeStoreValue
    {
        public string DisplayString;
        public FilteredTrackListModel.TrackFilter TrackFilter;
        public TestExpandRowHandler TestExpandRowHandler;
    }
}
