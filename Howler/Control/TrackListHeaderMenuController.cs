using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using Howler.Util;

namespace Howler.Control
{
    class TrackListHeaderMenuController
    {
        public Menu View { get; private set; }

        public TrackProperty? ClickedProperty { get; set; }

        public TrackListHeaderMenuController(BaseTrackListViewController trackListViewController)
        {
            var visibleColumns = trackListViewController.GetColumnTrackProperties().ToHashSet();

            Menu headerMenu = new Menu();

            foreach (TrackProperty trackProperty in Enum.GetValues(typeof(TrackProperty)))
            {
                var trackPropertyMenuItem = new CheckMenuItem(Extensions.GetEnumDescription(trackProperty));
                if (visibleColumns.Contains(trackProperty))
                    trackPropertyMenuItem.Active = true;
                TrackProperty property = trackProperty;
                trackPropertyMenuItem.Toggled += (sender, args) =>
                    {
                        if (trackPropertyMenuItem.Active)
                            trackListViewController.InsertColumn(property, ClickedProperty);
                        else
                            trackListViewController.RemoveColumn(property);
                    };
                headerMenu.Append(trackPropertyMenuItem);
            }

            View = headerMenu;
            View.ShowAll();
        }
    }
}
