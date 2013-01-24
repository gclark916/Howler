using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Howler.Gui
{
    public partial class MainWindow : Gtk.Window
    {
        public MainWindow() : base(Gtk.WindowType.Toplevel)
        {
            this.Name = "MainWindow";
            this.WindowPosition = ((global::Gtk.WindowPosition)(4));
            if ((this.Child != null))
            {
                this.Child.ShowAll();
            }
            this.DefaultWidth = 400;
            this.DefaultHeight = 300;
            this.Show();
        }
    }
}
