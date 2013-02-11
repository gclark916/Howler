﻿using Gtk;

namespace Howler.Gui
{
    class SourceTreeView : TreeView
    {
    }

    class SourceCellRenderer : CellRendererText
    {
        public SourceCellRenderer()
        {
            Ellipsize = Pango.EllipsizeMode.None;
            Size = 1;
            Font = "Segoe UI";
            FixedHeightFromFont = 0;
            Ypad = 0;
        }
    }

    class SourceTreeViewColumn : TreeViewColumn
    {
        public SourceTreeViewColumn()
        {
            MinWidth = 200;
            FixedWidth = 200;
            Sizing = TreeViewColumnSizing.Fixed;
        }
    }
}
