using System;
using Gtk;
using Howler.Gui;

namespace Howler.Control
{
    class MainController
    {
        public static void Main(string[] args)
        {
            Application.Init();
            MainWindow win = new MainWindow();
            win.Show();
            Application.Run();
        }
    }
}
