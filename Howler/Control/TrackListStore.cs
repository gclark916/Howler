using System.Collections.Generic;
using System.Linq;
using Gtk;
using Howler.Core.MediaLibrary.Entities;
using Howler.Core.Playback;

namespace Howler.Control
{
    class TrackListStore : ListStore, ITrackListModel
    {
        private readonly Dictionary<Track, TreeIter> _trackIters = new Dictionary<Track, TreeIter>();

        public Track CurrentTrack { get; protected set; }

        public TrackListStore(IEnumerable<Track> tracks)
            : base(typeof(Track), typeof(int))
        {
            SetNewPlaylist(tracks);
        }

        public void HandleTrackChanged(TrackChangedEventArgs args)
        {
            TreeIter iter = new TreeIter();
            CurrentTrack = args.NewTrack;
            bool exists = args.NewTrack != null && _trackIters.TryGetValue(args.NewTrack, out iter);
            if (exists)
            {
                EmitRowChanged(GetPath(iter), iter);
            }

            exists = args.OldTrack != null && _trackIters.TryGetValue(args.OldTrack, out iter);
            if (exists)
            {
                EmitRowChanged(GetPath(iter), iter);
            }
        }

        public void SetNewPlaylist(IEnumerable<Track> tracks)
        {
            Clear();
            _trackIters.Clear();
            int index = 0;
            foreach (Track track in tracks)
            {
                TreeIter trackIter = AppendValues(track, index++);
                _trackIters.Add(track, trackIter);
            }
        }
    }
}
