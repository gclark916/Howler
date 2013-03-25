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
        private readonly NowPlayingTrackListViewController _nowPlayingTrackListViewController;
        private readonly PlayerControlPanelController _playerControlPanelController;
        private readonly CoverArtPanel _coverArtPanel;

        MainController()
        {
            GLib.ExceptionManager.UnhandledException += args =>
            {
                Console.Write(args.ToString());
            };
            _collection = new Collection();
            //_collection.ImportDirectory("F:\\Music\\");
            _collection.ImportDirectory("D:\\Google Music\\");
            //_collection.ImportDirectory("F:\\Music\\Death Grips\\");

            AudioPlayer audioPlayer = new AudioPlayer();
            _filteredTrackListViewController = new FilteredTrackListViewController(_collection, audioPlayer);
            _sourceTreeViewController = new SourceTreeViewController(_filteredTrackListViewController, _collection);
            _playerControlPanelController = new PlayerControlPanelController(audioPlayer);
            _nowPlayingTrackListViewController = new NowPlayingTrackListViewController(audioPlayer);
            _coverArtPanel = new CoverArtPanel(audioPlayer, _filteredTrackListViewController, _nowPlayingTrackListViewController);

            VPaned vPaned = new VPaned();
            vPaned.Pack1(_nowPlayingTrackListViewController.View, true, true);
            vPaned.Pack2(_coverArtPanel, true, true);

            _window = new MainWindow();
            _window.DeleteEvent += (o, args) => Application.Quit();

            _window.AddWest(_sourceTreeViewController.View);
            _window.AddCenter(_filteredTrackListViewController.View);
            _window.AddEast(vPaned);
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
