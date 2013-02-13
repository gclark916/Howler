using Gtk;

namespace Howler.Gui
{
    class TracksListView : TreeView
    {
    }

    class TracksCellRenderer : CellRendererText
    {
        public TracksCellRenderer()
        {
            Ellipsize = Pango.EllipsizeMode.End;
            Size = 1;
            Font = "Segoe UI";
            Ypad = 0;
        }
    }

    class TracksListViewColumn : TreeViewColumn
    {
        public TracksListViewColumn(string title)
        {
            Title = title;
            Resizable = true;
            Expand = false;
            MinWidth = 10;
            FixedWidth = 200;
            Sizing = TreeViewColumnSizing.Fixed;
            Reorderable = true;
        }
    }
}
