using Gtk;

namespace Howler.Gui
{
    class TracksNodeView : TreeView
    {
    }

    class TracksCellRenderer : CellRendererText
    {
        public TracksCellRenderer()
        {
            Ellipsize = Pango.EllipsizeMode.End;
            Size = 1;
            Font = "Segoe UI";
            FixedHeightFromFont = 0;
            Ypad = 0;
        }
    }

    class TracksTreeViewColumn : TreeViewColumn
    {
        public TracksTreeViewColumn(string title)
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
