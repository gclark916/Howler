using System;
using System.Linq;
using Gdk;
using Gtk;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Howler.Gui
{
    class TrackListView : TreeView
    {
        public readonly TrackListViewTooltip Tooltip;
        private DateTime _scrollStarted;
        private uint _dragStartTime = 0;
        private double _dragStartX;
        private double _dragStartY;
        private TreePath _dragStartPath;

        public TrackListView()
        {
            HeadersClickable = true;
            RulesHint = true;
            FixedHeightMode = true;
            _scrollStarted = DateTime.MinValue;
            Tooltip = new TrackListViewTooltip(this);
            Selection.Mode = SelectionMode.Multiple;
        }

        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            Console.WriteLine("Button pressed");
            if (evnt.Button == 1)
            {
                _dragStartTime = evnt.Time;
                GetPathAtPos((int) evnt.X, (int) evnt.Y, out _dragStartPath);
            }
            return base.OnButtonPressEvent(evnt);
        }

        protected override bool OnButtonReleaseEvent(EventButton evnt)
        {
            if (evnt.Button == 1)
            {
                if (evnt.Time - _dragStartTime > 500)
                {
                    TreePath endPath;
                    if (!GetPathAtPos((int) evnt.X, (int) evnt.Y, out endPath))
                    {
                        GetPathAtPos(1, 1, out endPath);
                    }
                    if (_dragStartPath.Compare(endPath) < 0)
                        Selection.SelectRange(_dragStartPath, endPath);
                    else
                        Selection.SelectRange(endPath, _dragStartPath);
                }

                _dragStartTime = 0;
                Console.WriteLine("Button released {0}", _dragStartTime);
            }
            return base.OnButtonReleaseEvent(evnt);
        }

        protected override bool OnLeaveNotifyEvent(EventCrossing evnt)
        {
            /*if (evnt.Time - _dragStartTime > 500)
            {
                GLib.Timeout.Add(500, MultipleSelectOutsideWindowHander);
            }*/
            return base.OnLeaveNotifyEvent(evnt);
        }

        private bool MultipleSelectOutsideWindowHander()
        {
            int x, y;
            GetPointer(out x, out y);
            Console.WriteLine("x {0} y {1}", x, y);
            return _dragStartTime != 0;
        }

        protected override bool OnScrollEvent(EventScroll evnt)
        {
            _scrollStarted = DateTime.Now;
            return base.OnScrollEvent(evnt);
        }

        protected override bool OnMotionNotifyEvent(EventMotion evnt)
        {
            // First check if we need to draw a box for multiple selection via dragging
            if (_dragStartTime != 0 && evnt.Time - _dragStartTime > 500)
            {
                Console.WriteLine("Motion {0}", _dragStartTime);
                TreePath endPath;
                GetPathAtPos((int)evnt.X, (int)evnt.Y, out endPath);
                if (!GetPathAtPos((int)evnt.X, (int)evnt.Y, out endPath))
                {
                    GetPathAtPos(1, 1, out endPath);
                }
                if (_dragStartPath.Compare(endPath) < 0)
                {
                    Selection.UnselectAll();
                    Selection.SelectRange(_dragStartPath, endPath);
                }
                else
                {
                    Selection.UnselectAll();
                    Selection.SelectRange(endPath, _dragStartPath);
                }
            }

            // Now start logic for tooltips
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

    class TrackListViewTooltip : Window
    {
        protected internal readonly Label Label;
        private readonly TrackListView _trackListView;

        public TrackListViewTooltip(TrackListView trackListView) : base(WindowType.Popup)
        {
            _trackListView = trackListView;

            Label = new Label();
            Label.SetAlignment(0, 0.5f);
            Add(Label);
            Label.Show();

            BorderWidth = 1;
            Resizable = false;
            AddEvents((int) (EventMask.ScrollMask | EventMask.ButtonPressMask | EventMask.FocusChangeMask));

            _trackListView.FocusOutEvent += (o, args) => Hide();
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
            return _trackListView.ProcessEvent(evnt);
        }

        protected override bool OnFocusOutEvent(EventFocus evnt)
        {
            Hide();
            return _trackListView.ProcessEvent(evnt);
        }

        protected override bool OnScrollEvent(EventScroll evnt)
        {
            Hide();
            return _trackListView.ProcessEvent(evnt);
        }
    }

    class TrackCellRenderer : CellRendererText
    {
        public TrackCellRenderer()
        {
            Ellipsize = Pango.EllipsizeMode.End;
            Size = 1;
            Font = "Segoe UI";
            Ypad = 0;
        }
    }

    class TrackListViewColumn : TreeViewColumn
    {
        public TrackListViewColumn(string title)
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
