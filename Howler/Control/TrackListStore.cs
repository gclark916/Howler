using System.Collections.Generic;
using Gtk;
using Howler.Core.MediaLibrary.Entities;
using Howler.Core.Playback;

namespace Howler.Control
{
    class TrackListStore : ListStore
    {
        private readonly Dictionary<Track, TreeIter> _trackIters = new Dictionary<Track, TreeIter>();

        public Track CurrentTrack { get; private set; }

        public TrackListStore(IEnumerable<Track> tracks)
            : base(typeof(Track))
        {
            foreach (Track track in tracks)
            {
                TreeIter trackIter = AppendValues(track);
                _trackIters.Add(track, trackIter);
            }
        }

        public void HandleTrackChanged(TrackChangedHandlerArgs args)
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

        public void InsertTracks(int position, IEnumerable<Track> tracks)
        {
            int trackIndex = position;
            foreach (var track in tracks)
            {
                var iter = InsertWithValues(trackIndex, track);
                _trackIters.Add(track, iter);
            }
        }
    }
}
