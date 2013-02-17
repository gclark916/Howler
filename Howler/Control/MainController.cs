using System;
using Gtk;
using Howler.Core.Playback;
using Howler.Gui;
using Howler.Core.Database;

namespace Howler.Control
{
    class MainController
    {
        private readonly MainWindow _window;
        private readonly Collection _collection;
        private readonly TracksListViewController _tracksListViewController;
        private readonly SourceTreeViewController _sourceTreeViewController;
        private Widget _nowPlayingPanel;
        private PlayerControlPanelController _playerControlPanelController;

        MainController()
        {
            GLib.ExceptionManager.UnhandledException += args =>
            {
                Console.Write(args.ToString());
            };
            _collection = new Collection();
            //_collection.ImportDirectory("F:\\Google Music\\");
            //_collection.ImportDirectory("F:\\Music\\Death Grips\\Exmilitary");

            AudioPlayer audioPlayer = new AudioPlayer();
            _tracksListViewController = new TracksListViewController(_collection, audioPlayer);
            _sourceTreeViewController = new SourceTreeViewController(_tracksListViewController, _collection);
            _playerControlPanelController = new PlayerControlPanelController(audioPlayer);
            _nowPlayingPanel = new Label();

            _window = new MainWindow();
            _window.DeleteEvent += (o, args) => Application.Quit();

            _window.AddWest(_sourceTreeViewController.View);
            _window.AddCenter(_tracksListViewController.View);
            _window.AddEast(_nowPlayingPanel);
            _window.AddSouth(_playerControlPanelController.View);

            _window.ShowAll();
        }

        public void Run()
        {
            Application.Run();
        }

        public static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("GTK_SHARP_DEBUG", "");
            Application.Init();
            MainController controller = new MainController();
            controller.Run();
        }
    }
}
