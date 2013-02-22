using Gtk;

namespace Howler.Gui
{
    public class MainWindow : Window
    {
        private readonly VBox _vBox;
        private readonly Alignment _west;
        private readonly Alignment _center;
        private readonly Alignment _east;
        private readonly Alignment _north;
        private readonly Alignment _south;
        private readonly HPaned _westAndRestHPaned;
        private readonly HPaned _centerAndEastHPaned;

        public MainWindow() : base(WindowType.Toplevel)
        {
            Name = "MainWindow";
            WindowPosition = ((WindowPosition)(4));
            DefaultWidth = 1280;
            DefaultHeight = 800;

            
            _north = new Alignment(0, 0, 1, 1);
            _south = new Alignment(0, 0, 1, 1);
            _west = new Alignment(0, 0, 1, 1);
            _east = new Alignment(0, 0, 1, 1);
            _center = new Alignment(0, 0, 1, 1);

            _vBox = new VBox(false, 0);
            _westAndRestHPaned = new HPaned();
            _westAndRestHPaned.Pack1(_west, false, false);
            _centerAndEastHPaned = new HPaned();
            _westAndRestHPaned.Pack2(_centerAndEastHPaned, true, true);
            _centerAndEastHPaned.Pack1(_center, true, true);
            _centerAndEastHPaned.Pack2(_east, true, true);

            _centerAndEastHPaned.Position = 1000;

            _vBox.PackStart(_north, false, false, 0);
            _vBox.PackEnd(_south, false, false, 0);
            _vBox.PackEnd(_westAndRestHPaned, true, true, 0);

            Add(_vBox);

            ShowAll();
        }

        public void AddWest(Widget widget)
        {
            Widget old = _west.Child;
            if (old != null)
            {
                _west.Remove(old);
            }

            _west.Add(widget);
        }

        public void AddEast(Widget widget)
        {
            Widget old = _east.Child;
            if (old != null)
            {
                _east.Remove(old);
            }

            _east.Add(widget);
        }

        public void AddCenter(Widget widget)
        {
            Widget old = _center.Child;
            if (old != null)
            {
                _center.Remove(old);
            }

            _center.Add(widget);
        }

        public void AddSouth(Widget widget)
        {
            Widget old = _south.Child;
            if (old != null)
            {
                _south.Remove(old);
            }

            _south.Add(widget);
        }

        public void AddNorth(Widget widget)
        {
            Widget old = _north.Child;
            if (old != null)
            {
                _north.Remove(old);
            }

            _north.Add(widget);
        }
    }
}
