using Gtk;
using Howler.Gui;
using Howler.Core.Database;

namespace Howler.Control
{
    class MainController
    {
        private readonly MainWindow _window;
        private readonly Collection _collection;
        private readonly TracksNodeViewController _tracksNodeViewController;
        private readonly SourceTreeViewController _sourceTreeViewController;

        MainController()
        {
            _collection = new Collection();
            //_collection.ImportDirectory("F:\\Google Music\\");
            //_collection.ImportDirectory("F:\\Music\\Death Grips\\Exmilitary");

            _tracksNodeViewController = new TracksNodeViewController(_collection);
            _sourceTreeViewController = new SourceTreeViewController(_tracksNodeViewController, _collection);

            _window = new MainWindow();
            _window.DeleteEvent += (o, args) => Application.Quit();

            HPaned hPaned = new HPaned();
            hPaned.Add1(_sourceTreeViewController.View);
            hPaned.Add2(_tracksNodeViewController.View);
            _window.Add(hPaned);

            _window.ShowAll();
        }

        public void Run()
        {
            Application.Run();
        }

        public static void Main(string[] args)
        {
            Application.Init();
            MainController controller = new MainController();
            controller.Run();
        }
    }
}
