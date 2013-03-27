using System;
using Gtk;
using Howler.Core.MediaLibrary;
using Howler.Core.Playback;
using Howler.Gui;

namespace Howler.Control
{
    class MainWindowController
    {
        private readonly MainWindow _window;
        private readonly Collection _collection;
        private readonly FilteredTrackListViewController _filteredTrackListViewController;
        private readonly SourceTreeViewController _sourceTreeViewController;
        private readonly PlayerControlPanelController _playerControlPanelController;
        private readonly NowPlayingPanelController _nowPlayingPanelController;

        public MainWindowController()
        {
            GLib.ExceptionManager.UnhandledException += args =>
            {
                Console.Write(args.ToString());
            };
            _collection = new Collection();

            AudioPlayer audioPlayer = new AudioPlayer();
            _filteredTrackListViewController = new FilteredTrackListViewController(_collection, audioPlayer);
            _sourceTreeViewController = new SourceTreeViewController(_filteredTrackListViewController, _collection);
            _playerControlPanelController = new PlayerControlPanelController(audioPlayer);
            _nowPlayingPanelController = new NowPlayingPanelController(audioPlayer, _filteredTrackListViewController);

            _window = new MainWindow();
            _window.DeleteEvent += (o, args) => Application.Quit();

            _window.AddWest(_sourceTreeViewController.View);
            _window.AddCenter(_filteredTrackListViewController.View);
            _window.AddEast(_nowPlayingPanelController.View);
            _window.AddSouth(_playerControlPanelController.View);

            _window.ShowAll();
        }

        public void Run()
        {
            Application.Run();
        }
    }
}
