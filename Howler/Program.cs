using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using Howler.Control;

namespace Howler
{
    class Program
    {
        public static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("GTK_SHARP_DEBUG", "");
            Application.Init();
            MainWindowController windowController = new MainWindowController();
            windowController.Run();
        }
    }
}
