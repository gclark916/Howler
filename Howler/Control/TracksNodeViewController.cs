using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gtk;
using Howler.Gui;
using Howler.Core.Database;

namespace Howler.Control
{
    class TracksNodeViewController
    {
        public ScrolledWindow View { get; set; }
        ListStore Store;
        Collection Collection;

        delegate string StringPropertySelector(Track track);

        public TracksNodeViewController(Collection collection)
        {
            Collection = collection;
            View = new ScrolledWindow();
            TreeView treeView = new TreeView();
            treeView.HeadersClickable = true;
            treeView.RulesHint = true;

            AddTextColumn(t => t.Title, "Title", treeView);
            AddTextColumn(t => t.Artists == null || t.Artists.Count == 0 ? null : String.Join("; ", t.Artists.Select(a => a.Name)), "Artist", treeView);
            AddTextColumn(t => t.Album == null ? null : t.Album.Title, "Album", treeView);

            IEnumerable<PropertyInfo> stringProperties = typeof(Track).GetProperties()
                .Where(p => p.PropertyType.IsEquivalentTo(typeof(string)));

            foreach (PropertyInfo property in stringProperties)
                AddTextColumnUsingStringProperty(property, property.Name, treeView);

            Store = new ListStore(typeof(Track));

            IEnumerable<Track> tracks = collection.GetTracks();
            foreach (Track track in tracks)
            {
                Store.AppendValues(track);
            }

            treeView.Model = Store;

            View.Add(treeView);
        }


        private void AddTextColumn(StringPropertySelector selector, string columnName, TreeView treeView)
        {
            TracksTreeViewColumn genericColumn = new TracksTreeViewColumn(columnName);

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
            TracksTreeViewColumn genericColumn = new TracksTreeViewColumn(columnName);

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
