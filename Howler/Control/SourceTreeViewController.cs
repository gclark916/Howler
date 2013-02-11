using System;
using System.Linq;
using Howler.Core.Database;
using Howler.Gui;
using Gtk;

namespace Howler.Control
{
    class SourceTreeViewController
    {
        public ScrolledWindow View { get; private set; }
        private readonly SourceTreeView _sourceTreeView;
        private readonly TracksNodeViewController _tracksNodeViewController;

        public SourceTreeViewController(TracksNodeViewController tracksNodeViewController, Collection collection)
        {
            _tracksNodeViewController = tracksNodeViewController;

            _sourceTreeView = new SourceTreeView();
            SourceTreeViewColumn sourceColumn = new SourceTreeViewColumn();
            SourceCellRenderer sourceCellRenderer = new SourceCellRenderer();
            sourceColumn.PackStart(sourceCellRenderer, true);
            sourceColumn.SetCellDataFunc(sourceCellRenderer,
                (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) =>
                {
                    SourceTreeStoreValue value = (SourceTreeStoreValue)model.GetValue(iter, 0);
                    ((CellRendererText)cell).Text = value.DisplayString;
                });

            _sourceTreeView.Selection.Changed += (o, args) =>
            {
                var treeSelection = o as TreeSelection;
                if (treeSelection == null)
                    return;

                TreeModel model;
                TreeIter iter;
                treeSelection.GetSelected(out model, out iter);
                SourceTreeStoreValue value = (SourceTreeStoreValue)model.GetValue(iter, 0);
                if (value != null)
                    _tracksNodeViewController.FilterStore(value.TrackFilter);
            };

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

                foreach (Album album in someArtist.Album)
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


            _sourceTreeView.AppendColumn(sourceColumn);

            _sourceTreeView.Model = store;

            View = new ScrolledWindow {_sourceTreeView};
            View.SetSizeRequest(200, -1);
        }
    }

    class SourceTreeStoreValue
    {
        public string DisplayString;
        public TracksNodeViewController.TrackFilter TrackFilter;
        public TestExpandRowHandler TestExpandRowHandler;
    }
}
