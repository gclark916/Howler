using Howler.Core.MediaLibrary.Entities;
using Howler.Core.Playback;
using Gtk;

namespace Howler.Control
{
    interface ITrackListModel : TreeModel, TreeSortable
    {
        void HandleTrackChanged(TrackChangedHandlerArgs args);
        Track CurrentTrack { get; set; }
    }
}
