using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Howler.Gui
{
    class TracksNodeView : Gtk.TreeView
    {
        public TracksNodeView()
            : base()
        {
        }
    }

    class TracksCellRenderer : Gtk.CellRendererText
    {
        public TracksCellRenderer()
            : base()
        {
            Ellipsize = Pango.EllipsizeMode.End;
            Size = 1;
            Font = "Segoe UI";
            FixedHeightFromFont = 0;
            Ypad = 0;
        }
    }
}
