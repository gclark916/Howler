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
            treeView.EnableTreeLines = true;

            AddTextColumn(t => t.Title, "Title", treeView);
            AddTextColumn(t => t.Artists == null || t.Artists.Count == 0 ? null : String.Join("; ", t.Artists.Select(a => a.Name)), "Artist", treeView);
            AddTextColumn(t => t.Album == null ? null : t.Album.Title, "Album", treeView);

            IEnumerable<PropertyInfo> stringProperties = typeof(Track).GetProperties()
                .Where(p => p.PropertyType.IsEquivalentTo(typeof(string)));

            foreach (PropertyInfo property in stringProperties)
                AddTextColumnUsingStringProperty(property, property.Name, treeView);

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
            TracksTreeViewColumn genericColumn = new TracksTreeViewColumn(columnName);

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

        private void AddTextColumnUsingStringProperty(PropertyInfo property, string columnName, Gtk.TreeView treeView)
        {
            TracksTreeViewColumn genericColumn = new TracksTreeViewColumn(columnName);

            Gui.TracksCellRenderer pathCellTextRenderer = new Gui.TracksCellRenderer();
            genericColumn.PackStart(pathCellTextRenderer, true);
            genericColumn.SetCellDataFunc(pathCellTextRenderer,
                (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter) =>
                {
                    Track track = (Track)model.GetValue(iter, 0);
                    (cell as Gtk.CellRendererText).Text = (string)property.GetGetMethod().Invoke(track, null);
                });

            treeView.AppendColumn(genericColumn);
        }
    }
}
