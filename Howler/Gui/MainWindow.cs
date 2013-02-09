
namespace Howler.Gui
{
    public class MainWindow : Gtk.Window
    {
        public MainWindow() : base(Gtk.WindowType.Toplevel)
        {
            Name = "MainWindow";
            WindowPosition = ((Gtk.WindowPosition)(4));
            if ((Child != null))
            {
                Child.ShowAll();
            }
            DefaultWidth = 400;
            DefaultHeight = 300;
            Show();
        }
    }
}
