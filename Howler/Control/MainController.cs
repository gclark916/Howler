using System;
using Gtk;
using Howler.Gui;
using Howler.Core.Database;

namespace Howler.Control
{
    class MainController
    {
        MainWindow window;
        Collection collection;

        MainController()
        {
            collection = new Collection();

            window = new MainWindow();
            window.Show();

            window.DeleteEvent += (object o, DeleteEventArgs args) => { Application.Quit(); };

            collection.ImportDirectory("F:\\Music\\");
        }

        public static void Main(string[] args)
        {
            Application.Init();
            MainController controller = new MainController();
            Application.Run();
        }
    }
}
