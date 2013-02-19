﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Gdk;
using Gtk;
using Howler.Core.Playback;
using Howler.Gui;
using Howler.Core.Database;
using Howler.Util;

namespace Howler.Control
{
    class TracksListViewController
    {
        private readonly AudioPlayer _audioPlayer;

        public ScrolledWindow View { get; private set;  }
        private readonly TracksListView _tracksListView;
        private readonly TracksListViewSettings _settings;

        delegate string StringPropertySelector(Track track);
        internal delegate int TrackComparer(Track track1, Track track2);

        public delegate bool TrackFilter(Track track);

        public TracksListViewController(Collection collection, AudioPlayer audioPlayer)
        {
            _settings = new TracksListViewSettings();
            _settings.Reload();
            _audioPlayer = audioPlayer;

            _tracksListView = new TracksListView
                {
                    HeadersClickable = true,
                    RulesHint = true
                };

            _tracksListView.RowActivated += TracksListViewOnRowActivated;

            _tracksListView.Model = new TrackListModel(collection);

            int sortColumnId = 0;
            foreach (TrackProperty property in _settings.ColumnPropertyArray)
            {
                AddColumn(property, sortColumnId++);
            }

            _audioPlayer.TrackChanged += AudioPlayerOnTrackChanged;

            ((TreeModelSort) _tracksListView.Model).DefaultSortFunc = DefaultSortFunc;
            ((TreeModelSort) _tracksListView.Model).SetSortColumnId(2, SortType.Ascending);

            View = new ScrolledWindow {_tracksListView};

            _tracksListView.AddEvents((int) EventMask.AllEventsMask);
        }

        private static int DefaultSortFunc(TreeModel model, TreeIter iter1, TreeIter iter2)
        {
            //TODO: add discnumber comparison
            Track track1 = (Track)model.GetValue(iter1, 0);
            Track track2 = (Track)model.GetValue(iter2, 0);
            int result = string.Compare(string.Join("", track1.Artists), string.Join("", track2.Artists), StringComparison.CurrentCulture);
            if (result == 0)
                result = string.Compare(string.Join("", track1.Album.Artists), string.Join("", track2.Album.Artists),
                                        StringComparison.CurrentCulture);
            if (result == 0)
                result = string.Compare(string.Join("", track1.Album.Title), string.Join("", track2.Album.Title),
                                        StringComparison.CurrentCulture);
            if (result == 0)
            {
                Int64 trackNumber1 = track1.TrackNumber ?? Int64.MinValue;
                Int64 trackNumber2 = track2.TrackNumber ?? Int64.MinValue;
                result = trackNumber1.CompareTo(trackNumber2);
            }

            return result;
        }

        private void TracksListViewOnRowActivated(object o, RowActivatedArgs args)
        {
            TracksListView tracksListView = o as TracksListView;
            if (tracksListView == null)
                return;

            int numTracks = tracksListView.Model.IterNChildren();
            Track[] trackArray = new Track[numTracks];
            TreeIter iter;
            int selectedTrackIndex = args.Path.Indices[0];
            tracksListView.Model.GetIterFirst(out iter);

            uint trackIndex = 0;
            bool valid = tracksListView.Model.GetIterFirst(out iter);
            while (valid)
            {
                Track track = (Track) tracksListView.Model.GetValue(iter, 0);
                trackArray[trackIndex++] = track;
                valid = tracksListView.Model.IterNext(ref iter);
            }
            _audioPlayer.ReplacePlaylistAndPlay(trackArray, (uint)selectedTrackIndex);
        }

        private void AudioPlayerOnTrackChanged(object sender, TrackChangedHandlerArgs args)
        {
            var trackListModel = _tracksListView.Model as TrackListModel;
            Debug.Assert(trackListModel != null, "trackListModel != null");

            trackListModel.HandleTrackChanged(args);
        }

        public void FilterStore(TrackFilter trackFilter)
        {
            var trackListModel = _tracksListView.Model as TrackListModel;
            Debug.Assert(trackListModel != null, "trackListModel != null");

            var filteredModel = trackListModel.Model as TreeModelFilter;
            Debug.Assert(filteredModel != null, "filteredModel != null");

            trackListModel.TrackFilter = trackFilter;
            filteredModel.Refilter();
        }

        private void AddTextColumn(StringPropertySelector selector, string columnName, int sortColumnId)
        {
            AddTextColumn(selector, columnName, sortColumnId, (track1, track2) => string.Compare(selector(track1), selector(track2), StringComparison.CurrentCulture));
        }

        private void AddTextColumn(StringPropertySelector selector, string columnName, int sortColumnId, TrackComparer comparer)
        {
            TracksListViewColumn genericColumn = new TracksListViewColumn(columnName);

            TracksCellRenderer pathCellTextRenderer = new TracksCellRenderer();
            genericColumn.PackStart(pathCellTextRenderer, true);
            genericColumn.SetCellDataFunc(pathCellTextRenderer,
                (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) =>
                    {
                        Track track = (Track)model.GetValue(iter, 0);
                        Track playingTrack = ((TrackListModel) model).CurrentTrack;
                        bool playing = playingTrack != null && track.Equals(playingTrack);
                        ((CellRendererText) cell).Text = selector(track);
                        ((CellRendererText) cell).Weight = playing ? 800 : 400;
                    });

            if (sortColumnId >= 0)
            {
                genericColumn.SortColumnId = sortColumnId;
                ((TreeModelSort)_tracksListView.Model).SetSortFunc(sortColumnId, (model, iter1, iter2) =>
                    {
                        Track track1 = (Track) model.GetValue(iter1, 0);
                        Track track2 = (Track) model.GetValue(iter2, 0);
                        int result = comparer(track1, track2);
                        if (result == 0)
                            result = DefaultSortFunc(model, iter1, iter2);
                        return result;
                    });
            }

            _tracksListView.AppendColumn(genericColumn);
        }

        private void AddColumn(TrackProperty property, int sortColumnId)
        {
            string description = Extensions.GetEnumDescription(property);
            switch (property)
            {
                case TrackProperty.Album:
                    AddTextColumn(t => t.Album != null ? t.Album.Title : null, description, sortColumnId);
                    break;
                case TrackProperty.Artist:
                    AddTextColumn(t => t.Artists == null || t.Artists.Count == 0 ? null : String.Join("; ", t.Artists.Select(a => a.Name)), description, sortColumnId);
                    break;
                case TrackProperty.AlbumArtist:
                    AddTextColumn(t => (t.Album == null || t.Album.Artists == null || t.Artists.Count == 0) ? null
                        : String.Join("; ", t.Album.Artists.Select(a => a.Name)), description, sortColumnId);
                    break;
                case TrackProperty.Duration:
                    AddTextColumn(t =>
                    {
                        TimeSpan timeSpan = TimeSpan.FromMilliseconds(t.Duration);
                        string length = t.Duration >= 1000*60*60
                            ? String.Format(CultureInfo.CurrentCulture, "{0}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds)
                            : String.Format(CultureInfo.CurrentCulture, "{0}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
                        return length;
                    }, description, sortColumnId);
                    break;
                case TrackProperty.Bitrate:
                    AddTextColumn(t => string.Format("{0} kbps", t.Bitrate), description, sortColumnId);
                    break;
                case TrackProperty.BitsPerSample:
                    AddTextColumn(t => t.BitsPerSample.ToString(CultureInfo.InvariantCulture), description, sortColumnId);
                    break;
                case TrackProperty.Bpm:
                    AddTextColumn(t => t.BPM.ToString(), description, sortColumnId);
                    break;
                case TrackProperty.ChannelCount:
                    AddTextColumn(t => t.ChannelCount.ToString(CultureInfo.InvariantCulture), description, sortColumnId);
                    break;
                case TrackProperty.Codec:
                    AddTextColumn(t => t.Codec, description, sortColumnId);
                    break;
                case TrackProperty.Date:
                    AddTextColumn(t => t.Date, description, sortColumnId);
                    break;
                case TrackProperty.DateAdded:
                    AddTextColumn(t => string.Format("{0} {1}", t.DateAdded.ToShortDateString(), t.DateAdded.ToLongTimeString()), description, sortColumnId);
                    break;
                case TrackProperty.DateLastPlayed:
                    AddTextColumn(t => t.DateLastPlayed.HasValue && t.DateLastPlayed.Value > DateTime.MinValue
                        ? string.Format("{0} {1}", t.DateLastPlayed.Value.ToShortDateString(), t.DateLastPlayed.Value.ToLongTimeString()) 
                        : null, 
                        description, sortColumnId);
                    break;
                /*case TrackProperty.DiscNumber:*/
                case TrackProperty.Genre:
                    AddTextColumn(t => t.Artists == null || t.Artists.Count == 0 ? null : String.Join("; ", t.Genres.Select(g => g.Name)), description, sortColumnId);
                    break;
                case TrackProperty.MusicBrainzAlbumId:
                    AddTextColumn(t => t.Album != null ? t.Album.MusicBrainzId : null, description, sortColumnId);
                    break;
                case TrackProperty.MusicBrainzReleaseId:
                    AddTextColumn(t => t.MusicBrainzId, description, sortColumnId);
                    break;
                case TrackProperty.Path:
                    AddTextColumn(t => t.Path, description, sortColumnId);
                    break;
                case TrackProperty.Playcount:
                    AddTextColumn(t => t.Playcount.ToString(CultureInfo.InvariantCulture), description, sortColumnId);
                    break;
                case TrackProperty.Rating:
                    // TODO: implement stars
                    AddTextColumn(t => t.Rating.ToString(), description, sortColumnId);
                    break;
                case TrackProperty.SampleRate:
                    AddTextColumn(t => string.Format("{0} Hz", t.SampleRate), description, sortColumnId);
                    break;
                case TrackProperty.Size:
                    AddTextColumn(t => string.Format("{0:N1} MB", (double)t.Size / (1024*1024)), description, sortColumnId);
                    break;
                case TrackProperty.Title:
                    AddTextColumn(t => t.Title, description, sortColumnId);
                    break;
                case TrackProperty.TrackNumber:
                    AddTextColumn(t => t.TrackNumber.ToString(), description, sortColumnId, (track1, track2) =>
                        {
                            var num1 = track1.TrackNumber ?? Int64.MinValue;
                            var num2 = track2.TrackNumber ?? Int64.MinValue;
                            return num1.CompareTo(num2);
                        });
                    break;
                default:
                    Console.WriteLine("Column not implemented for track property name {0}", description);
                    break;
            }
        }

        class TrackListModel : TreeModelSort
        {
            private TrackFilter _trackFilter = track => true;
            private readonly Dictionary<Track, TreeIter> _unfilteredTrackIters;

            public Track CurrentTrack { get; private set; }
            public TrackFilter TrackFilter
            {
                set { _trackFilter = value; }
            }

            public TrackListModel(Collection collection) : this(CreateListStoreAndDictionaryTuple(collection))
            {
            }

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

            private static Tuple<ListStore, Dictionary<Track, TreeIter>>  CreateListStoreAndDictionaryTuple(Collection collection)
            {
                var unfilteredModel = new ListStore(typeof(Track));

                var unfilteredTrackIters = new Dictionary<Track, TreeIter>();

                foreach (Track track in collection.GetTracks())
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

    class TracksListViewSettings : ApplicationSettingsBase
    {
        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public TrackProperty[] ColumnPropertyArray
        {
            get
            {
                return (TrackProperty[]) this["ColumnPropertyArray"];
            } 
            set
            {
                this["ColumnPropertyArray"] = (TrackProperty[]) value;
            }
        }

        public TracksListViewSettings()
        {
            ColumnPropertyArray = new[] {TrackProperty.TrackNumber, TrackProperty.Title, TrackProperty.Artist, TrackProperty.Album, TrackProperty.AlbumArtist, TrackProperty.Date, TrackProperty.Genre, TrackProperty.Rating, TrackProperty.Bitrate, TrackProperty.Size, TrackProperty.DateAdded, TrackProperty.Codec, TrackProperty.Path};
        }
    }

    internal enum TrackProperty
    {
        Album,
        [Description("Album Artist")]
        AlbumArtist, 
        Artist,
        [Description("BPM")]
        Bpm, 
        Bitrate,
        [Description("Bits per sample")]
        BitsPerSample,
        [Description("Channels")]
        ChannelCount, 
        Codec, 
        Date,
        [Description("Date Added")]
        DateAdded,
        [Description("Last Played")]
        DateLastPlayed,
        [Description("Disc #")]
        DiscNumber, 
        Duration, 
        Genre, 
        MusicBrainzReleaseId, 
        MusicBrainzAlbumId, 
        Path, 
        Playcount, 
        Rating,
        [Description("Sample rate")]
        SampleRate, 
        Size, 
        Title,
        [Description("Track #")]
        TrackNumber
    }
}
