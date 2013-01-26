using System;
using Gtk;
using Howler.Gui;
using Howler.Core.Database;

namespace Howler.Control
{
    class MainController
    {
        MainWindow Window;
        Collection Collection;
        TracksNodeViewController TracksNodeViewController;

        MainController()
        {
            Collection = new Collection();
            Collection.ImportDirectory("F:\\Google Music\\");

            TracksNodeViewController = new TracksNodeViewController(Collection);

            Window = new MainWindow();
            Window.DeleteEvent += (object o, DeleteEventArgs args) => { Application.Quit(); };

            Window.Add(TracksNodeViewController.View);

            Window.ShowAll();
        }

        public static void Main(string[] args)
        {
            Application.Init();
            MainController controller = new MainController();
            Application.Run();
        }
    }
}
