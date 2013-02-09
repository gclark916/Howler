using Gtk;
using Howler.Gui;
using Howler.Core.Database;

namespace Howler.Control
{
    class MainController
    {
        readonly MainWindow _window;
        readonly Collection _collection;
        readonly TracksNodeViewController _tracksNodeViewController;

        MainController()
        {
            _collection = new Collection();
            _collection.ImportDirectory("F:\\Google Music\\");

            _tracksNodeViewController = new TracksNodeViewController(_collection);

            _window = new MainWindow();
            _window.DeleteEvent += (object o, DeleteEventArgs args) => Application.Quit();

            _window.Add(_tracksNodeViewController.View);

            _window.ShowAll();
        }

        public static void Main(string[] args)
        {
            Application.Init();
            MainController controller = new MainController();
            Application.Run();
        }
    }
}
