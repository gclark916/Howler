using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gtk;
using Howler.Core.Database;
using Howler.Core.Playback;

namespace Howler.Control
{
    class TrackListModel : TreeModelSort
    {
        private TrackListViewController.TrackFilter _trackFilter = track => true;
        private readonly Dictionary<Track, TreeIter> _unfilteredTrackIters;

        public Track CurrentTrack { get; private set; }
        public TrackListViewController.TrackFilter TrackFilter
        {
            set
            {
                _trackFilter = value;
                ((TreeModelFilter)Model).Refilter();
            }
        }

        public TrackListModel(IEnumerable<Track> tracks)
            : this(CreateListStoreAndDictionaryTuple(tracks))
        { }

        public TrackListModel(Collection collection)
            : this(CreateListStoreAndDictionaryTuple(collection))
        { }

        private TrackListModel(Tuple<ListStore, Dictionary<Track, TreeIter>> tuple)
            : base(new TreeModelFilter(tuple.Item1, null))
        {
            _unfilteredTrackIters = tuple.Item2;
            ((TreeModelFilter)Model).VisibleFunc = (model, iter) =>
                {
                    Track track = (Track)model.GetValue(iter, 0);
                    return _trackFilter(track);
                };
        }

        private static Tuple<ListStore, Dictionary<Track, TreeIter>> CreateListStoreAndDictionaryTuple(Collection collection)
        {
            return CreateListStoreAndDictionaryTuple(collection.GetTracks());
        }

        private static Tuple<ListStore, Dictionary<Track, TreeIter>> CreateListStoreAndDictionaryTuple(IEnumerable<Track> tracks)
        {
            var unfilteredModel = new ListStore(typeof(Track));

            var unfilteredTrackIters = new Dictionary<Track, TreeIter>();

            foreach (Track track in tracks)
            {
                TreeIter trackIter = unfilteredModel.AppendValues(track);
                unfilteredTrackIters.Add(track, trackIter);
            }

            return new Tuple<ListStore, Dictionary<Track, TreeIter>>(unfilteredModel, unfilteredTrackIters);
        }

        public void HandleTrackChanged(TrackChangedHandlerArgs args)
        {
            var filteredModel = Model as TreeModelFilter;
            Debug.Assert(filteredModel != null, "filteredModel != null");

            TreeIter iter = new TreeIter();
            CurrentTrack = args.NewTrack;
            bool exists = args.NewTrack != null && _unfilteredTrackIters.TryGetValue(args.NewTrack, out iter);
            if (exists)
            {
                TreeIter filteredIter = filteredModel.ConvertChildIterToIter(iter);
                TreeIter newIter = ConvertChildIterToIter(filteredIter);
                EmitRowChanged(GetPath(newIter), newIter);
            }

            exists = args.OldTrack != null && _unfilteredTrackIters.TryGetValue(args.OldTrack, out iter);
            if (exists)
            {
                TreeIter filteredIter = filteredModel.ConvertChildIterToIter(iter);
                TreeIter oldIter = ConvertChildIterToIter(filteredIter);
                EmitRowChanged(GetPath(oldIter), oldIter);
            }
        }
    }
}