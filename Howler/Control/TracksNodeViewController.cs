using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Howler.Gui;
using Howler.Core.Database;

namespace Howler.Control
{
    class TracksNodeViewController
    {
        public Gtk.ScrolledWindow View { get; set; }
        Gtk.ListStore Store;
        Collection Collection;

        public TracksNodeViewController(Collection collection)
        {
            Collection = collection;
            View = new Gtk.ScrolledWindow();
            Gtk.TreeView treeView = new Gtk.TreeView();

            Gtk.TreeViewColumn pathColumn = new Gtk.TreeViewColumn();
            pathColumn.Title = "Path";

            Gtk.CellRendererText pathCellTextRenderer = new Gtk.CellRendererText();
            pathCellTextRenderer.Ellipsize = Pango.EllipsizeMode.End;
            pathColumn.PackStart(pathCellTextRenderer, true);
            pathColumn.SetCellDataFunc(pathCellTextRenderer, 
                (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter) =>
                {
                    Track track = (Track) model.GetValue(iter, 0);

                    (cell as Gtk.CellRendererText).Text = track.Path;
                });

            
            Gtk.CellRendererPixbuf pathCellRendererPixBuf = new Gtk.CellRendererPixbuf();
            pathCellRendererPixBuf.Pixbuf = new Gdk.Pixbuf("uparrow.png");
            pathCellRendererPixBuf.Mode = Gtk.CellRendererMode.Activatable;
            pathColumn.PackEnd(pathCellRendererPixBuf, false);

            Store = new Gtk.ListStore(typeof(Track));

            IEnumerable<Track> tracks = collection.GetTracks();
            foreach (Track track in tracks)
            {
                Store.AppendValues(track);
            }

            treeView.Model = Store;

            treeView.AppendColumn(pathColumn);

            View.Add(treeView);
        }
    }
}
