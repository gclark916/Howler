using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Howler.Gui;
using Howler.Core.Database;
using System.Linq.Expressions;

namespace Howler.Control
{
    class TracksNodeViewController
    {
        public Gtk.ScrolledWindow View { get; set; }
        Gtk.ListStore Store;
        Collection Collection;

        delegate string StringPropertySelector(Track track);

        public TracksNodeViewController(Collection collection)
        {
            Collection = collection;
            View = new Gtk.ScrolledWindow();
            Gtk.TreeView treeView = new Gtk.TreeView();
            treeView.HeadersClickable = true;

            AddTextColumn(t => t.Title, "Title", treeView);
            AddTextColumn(t => t.Artists == null || t.Artists.Count == 0 ? null : String.Join("; ", t.Artists.Select(a => a.Name)), "Artist", treeView);
            AddTextColumn(t => t.Album == null ? null : t.Album.Title, "Album", treeView);
            AddTextColumn(t => t.Path, "Path", treeView);

            Store = new Gtk.ListStore(typeof(Track));

            IEnumerable<Track> tracks = collection.GetTracks();
            foreach (Track track in tracks)
            {
                Store.AppendValues(track);
            }

            treeView.Model = Store;

            View.Add(treeView);
        }


        private void AddTextColumn(StringPropertySelector selector, string columnName, Gtk.TreeView treeView)
        {
            Gtk.TreeViewColumn genericColumn = new Gtk.TreeViewColumn();
            genericColumn.Title = columnName;
            genericColumn.Resizable = true;
            genericColumn.Expand = false;
            genericColumn.MinWidth = 10;
            genericColumn.FixedWidth = 200;
            genericColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;

            Gui.TracksCellRenderer pathCellTextRenderer = new Gui.TracksCellRenderer();
            genericColumn.PackStart(pathCellTextRenderer, true);
            genericColumn.SetCellDataFunc(pathCellTextRenderer,
                (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter) =>
                {
                    Track track = (Track)model.GetValue(iter, 0);
                    (cell as Gtk.CellRendererText).Text = selector.Invoke(track);
                });

            treeView.AppendColumn(genericColumn);
        }
    }
}
