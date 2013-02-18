﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using Gdk;
using Gtk;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Howler.Gui
{
    class TracksListView : TreeView
    {
        public readonly TracksListViewTooltip Tooltip;
        private DateTime _scrollStarted;

        public TracksListView()
        {
            _scrollStarted = DateTime.MinValue;
            Tooltip = new TracksListViewTooltip(this);
            FixedHeightMode = true;
        }

        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            return base.OnButtonPressEvent(evnt);
        }

        protected override bool OnScrollEvent(EventScroll evnt)
        {
            _scrollStarted = DateTime.Now;
            return base.OnScrollEvent(evnt);
        }

        protected override bool OnMotionNotifyEvent(EventMotion evnt)
        {
            if (!evnt.Window.Equals(BinWindow))
                goto hide;

            if (DateTime.Now.Subtract(TimeSpan.FromMilliseconds(500)) < _scrollStarted)
                goto hide;

            TreePath path;
            TreeViewColumn column;
            int cellX, cellY;
            if (!GetPathAtPos((int) evnt.X, (int) evnt.Y, out path, out column, out cellX, out cellY))
                goto hide;

            var cellRenderers = column.CellRenderers.Where(c =>
            {
                int startPos, width;
                column.CellGetPosition(c, out startPos, out width);
                return startPos < cellX && cellX < startPos + width;
            });

            var cellRendererText = (CellRendererText)cellRenderers.FirstOrDefault();

            TreeIter iter;
            Model.GetIter(out iter, path);
            column.CellSetCellData(Model, iter, false, false);

            if (cellRendererText != null)
            {
                Label label = Tooltip.Label;
                label.Text = cellRendererText.Text;
                label.SetPadding((int)cellRendererText.Xpad, (int)cellRendererText.Ypad);
                label.SizeRequest();
                label.Show();

                int listOriginX, listOriginY, labelWidth, labelHeight, labelLayoutOffsetX, labelLayoutOffsetY;
                BinWindow.GetOrigin(out listOriginX, out listOriginY);
                Rectangle cellRectangle = GetCellArea(path, column);
                var x = listOriginX + cellRectangle.X;
                var y = listOriginY + cellRectangle.Y;

                label.Layout.GetPixelSize(out labelWidth, out labelHeight);
                label.GetLayoutOffsets(out labelLayoutOffsetX, out labelLayoutOffsetY);
                labelWidth += labelLayoutOffsetX;
                labelWidth += (int)cellRendererText.Xpad;

                var height = cellRectangle.Height;
                Tooltip.Move(x, y);
                Tooltip.SetSizeRequest(labelWidth, height);
                Tooltip.Resize(labelWidth, height);

                if (labelWidth < cellRectangle.Width)
                    goto hide;
                Tooltip.Show();
                Console.WriteLine("{0} {1} {2} {3}", path, evnt.X, evnt.Y, cellRendererText.Text);
                goto returnBase;
            }

        hide:
            Tooltip.Hide();

        returnBase:
            return base.OnMotionNotifyEvent(evnt);
        }
    }

    class TracksListViewTooltip : Window
    {
        protected internal readonly Label Label;
        private readonly TracksListView _tracksListView;

        public TracksListViewTooltip(TracksListView tracksListView) : base(WindowType.Popup)
        {
            _tracksListView = tracksListView;

            Label = new Label();
            Label.SetAlignment(0, 0.5f);
            Add(Label);
            Label.Show();

            BorderWidth = 1;
            Resizable = false;
            AddEvents((int) (EventMask.ScrollMask | EventMask.ButtonPressMask | EventMask.FocusChangeMask));

            _tracksListView.FocusOutEvent += (o, args) => Hide();
        }

        protected override bool OnUnmapEvent(Event evnt)
        {
            Hide();
            return base.OnUnmapEvent(evnt);
        }

        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            // TODO: edit evnt or create new one so that _tracksListView will process it correctly
            Hide();
            return _tracksListView.ProcessEvent(evnt);
        }

        protected override bool OnFocusOutEvent(EventFocus evnt)
        {
            Hide();
            return _tracksListView.ProcessEvent(evnt);
        }

        protected override bool OnScrollEvent(EventScroll evnt)
        {
            Hide();
            return _tracksListView.ProcessEvent(evnt);
        }
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
