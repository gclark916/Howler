using System;
using Gtk;

namespace Howler.Control
{
    class MenuBarController
    {
        public MenuBar View { get; private set; }

        public MenuBarController()
        {
            MenuBar mb = new MenuBar();

            Menu filemenu = new Menu();
            MenuItem file = new MenuItem("File");
            file.Submenu = filemenu;

            ImageMenuItem importDirectoryMenuItem = new ImageMenuItem("Import Directory");
            importDirectoryMenuItem.Activated += ImportDirectoryMenuItemOnActivated;
            filemenu.Append(importDirectoryMenuItem);

            ImageMenuItem open = new ImageMenuItem(Stock.Open);
            filemenu.Append(open);

            SeparatorMenuItem sep = new SeparatorMenuItem();
            filemenu.Append(sep);

            ImageMenuItem exit = new ImageMenuItem(Stock.Quit);

            exit.Activated += (sender, args) => Application.Quit();
            filemenu.Append(exit);

            mb.Append(file);
            View = mb;
        }

        private void ImportDirectoryMenuItemOnActivated(object sender, EventArgs eventArgs)
        {
            var fileChooser = new FileChooserWidget(FileChooserAction.SelectFolder);
            fileChooser.SelectMultiple = true;
            var window = new Window(WindowType.Toplevel);
            window.Add(fileChooser);
            window.ShowAll();
        }
    }
}
