using System;
using Gtk;
using Howler.Gui;

namespace Howler.Control
{
    class MainController
    {
        MainWindow window;

        MainController()
        {
            window = new MainWindow();
            window.Show();

            window.DeleteEvent += (object o, DeleteEventArgs args) => { Application.Quit(); };
        }

        public static void Main(string[] args)
        {
            Application.Init();
            MainController controller = new MainController();
            Application.Run();
        }
    }
}
