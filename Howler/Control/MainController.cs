using System;
using Gtk;
using Howler.Core.MediaLibrary;
using Howler.Core.Playback;
using Howler.Gui;

namespace Howler.Control
{
    class MainController
    {
        private readonly MainWindow _window;
        private readonly Collection _collection;
        private readonly FilteredTrackListViewController _filteredTrackListViewController;
        private readonly SourceTreeViewController _sourceTreeViewController;
        private readonly Widget _nowPlayingPanel;
        private readonly PlayerControlPanelController _playerControlPanelController;

        MainController()
        {
            GLib.ExceptionManager.UnhandledException += args =>
            {
                Console.Write(args.ToString());
            };
            _collection = new Collection();
            //_collection.ImportDirectory("F:\\Google Music\\");
            //_collection.ImportDirectory("F:\\Music\\Death Grips\\");

            AudioPlayer audioPlayer = new AudioPlayer();
            _filteredTrackListViewController = new FilteredTrackListViewController(_collection, audioPlayer);
            _sourceTreeViewController = new SourceTreeViewController(_filteredTrackListViewController, _collection);
            _playerControlPanelController = new PlayerControlPanelController(audioPlayer);
            _nowPlayingPanel = new Label();

            _window = new MainWindow();
            _window.DeleteEvent += (o, args) => Application.Quit();

            _window.AddWest(_sourceTreeViewController.View);
            _window.AddCenter(_filteredTrackListViewController.View);
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
