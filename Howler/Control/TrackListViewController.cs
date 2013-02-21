using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Gdk;
using Gtk;
using Howler.Core.MediaLibrary;
using Howler.Core.MediaLibrary.Entities;
using Howler.Core.Playback;
using Howler.Gui;
using Howler.Util;

namespace Howler.Control
{
    class TrackListViewController
    {
        private readonly AudioPlayer _audioPlayer;

        public ScrolledWindow View { get; private set;  }
        private readonly TrackListView _trackListView;
        private readonly TracksListViewSettings _settings;

        delegate string StringPropertySelector(Track track);
        internal delegate int TrackComparer(Track track1, Track track2);

        public delegate bool TrackFilter(Track track);

        public TrackListViewController(Collection collection, AudioPlayer audioPlayer)
        {
            _settings = new TracksListViewSettings();
            _settings.Reload();
            if (_settings.ColumnPropertyArray == null || !_settings.ColumnPropertyArray.Any())
            {
                _settings.LoadDefaultColumnPropertyArray();
                _settings.Save();
            }
            if (_settings.ColumnWidths == null)
            {
                _settings.LoadDefaultColumnWidths();
                _settings.Save();
            }

            _audioPlayer = audioPlayer;

            _trackListView = new TrackListView
                {
                    HeadersClickable = true,
                    RulesHint = true
                };

            _trackListView.RowActivated += TrackListViewOnRowActivated;

            _trackListView.Model = new TrackListModel(collection);

            int sortColumnId = 0;
            int artistSortColumnId = -1;
            foreach (TrackProperty property in _settings.ColumnPropertyArray)
            {
                if (property == TrackProperty.Artist)
                    artistSortColumnId = sortColumnId;
                AddColumn(property, sortColumnId++);
            }

            _audioPlayer.TrackChanged += AudioPlayerOnTrackChanged;

            ((TreeModelSort) _trackListView.Model).DefaultSortFunc = DefaultSortFunc;
            if (artistSortColumnId >= 0)
                ((TreeModelSort) _trackListView.Model).SetSortColumnId(artistSortColumnId, SortType.Ascending);

            View = new ScrolledWindow {_trackListView};

            _trackListView.AddEvents((int) EventMask.AllEventsMask);
        }

        private static int DefaultSortFunc(TreeModel model, TreeIter iter1, TreeIter iter2)
        {
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
                uint discNumber1 = track1.DiscNumber ?? 0;
                uint discNumber2 = track2.DiscNumber ?? 0;
                result = discNumber1.CompareTo(discNumber2);
            }
            if (result == 0)
            {
                uint trackNumber1 = track1.TrackNumber ?? 0;
                uint trackNumber2 = track2.TrackNumber ?? 0;
                result = trackNumber1.CompareTo(trackNumber2);
            }

            return result;
        }

        private void TrackListViewOnRowActivated(object o, RowActivatedArgs args)
        {
            TrackListView trackListView = o as TrackListView;
            if (trackListView == null)
                return;

            int numTracks = trackListView.Model.IterNChildren();
            Track[] trackArray = new Track[numTracks];
            TreeIter iter;
            int selectedTrackIndex = args.Path.Indices[0];
            trackListView.Model.GetIterFirst(out iter);

            uint trackIndex = 0;
            bool valid = trackListView.Model.GetIterFirst(out iter);
            while (valid)
            {
                Track track = (Track) trackListView.Model.GetValue(iter, 0);
                trackArray[trackIndex++] = track;
                valid = trackListView.Model.IterNext(ref iter);
            }
            _audioPlayer.ReplacePlaylistAndPlay(trackArray, (uint)selectedTrackIndex);
        }

        private void AudioPlayerOnTrackChanged(object sender, TrackChangedHandlerArgs args)
        {
            var trackListModel = _trackListView.Model as TrackListModel;
            Debug.Assert(trackListModel != null, "trackListModel != null");

            trackListModel.HandleTrackChanged(args);
        }

        public void FilterStore(TrackFilter trackFilter)
        {
            var trackListModel = _trackListView.Model as TrackListModel;
            Debug.Assert(trackListModel != null, "trackListModel != null");

            trackListModel.TrackFilter = trackFilter;
        }

        private void AddTextColumn(StringPropertySelector selector, string columnName, int sortColumnId, int columnWidth)
        {
            AddTextColumn(selector, columnName, sortColumnId, (track1, track2) => string.Compare(selector(track1), selector(track2), StringComparison.CurrentCulture), columnWidth);
        }

        private void AddTextColumn(StringPropertySelector selector, string columnName, int sortColumnId, TrackComparer comparer, int columnWidth)
        {
            TrackListViewColumn genericColumn = new TrackListViewColumn(columnName) {FixedWidth = columnWidth};

            TrackCellRenderer pathCellTextRenderer = new TrackCellRenderer();
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
                ((TreeModelSort)_trackListView.Model).SetSortFunc(sortColumnId, (model, iter1, iter2) =>
                    {
                        Track track1 = (Track) model.GetValue(iter1, 0);
                        Track track2 = (Track) model.GetValue(iter2, 0);
                        int result = comparer(track1, track2);
                        if (result == 0)
                            result = DefaultSortFunc(model, iter1, iter2);
                        return result;
                    });
            }

            _trackListView.AppendColumn(genericColumn);
        }

        private void AddColumn(TrackProperty property, int sortColumnId)
        {
            string description = Extensions.GetEnumDescription(property);
            int columnWidth;
            if (!_settings.ColumnWidths.TryGetValue(property, out columnWidth))
                columnWidth = 200;
            switch (property)
            {
                case TrackProperty.Album:
                    AddTextColumn(t => t.Album != null ? t.Album.Title : null, description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Artist:
                    AddTextColumn(t => t.Artists == null || t.Artists.Count == 0 ? null : String.Join("; ", t.Artists.Select(a => a.Name)), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.AlbumArtist:
                    AddTextColumn(t => (t.Album == null || t.Album.Artists == null || t.Artists.Count == 0) ? null
                        : String.Join("; ", t.Album.Artists.Select(a => a.Name)), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Duration:
                    AddTextColumn(t =>
                    {
                        TimeSpan timeSpan = TimeSpan.FromMilliseconds(t.Duration);
                        string length = t.Duration >= 1000*60*60
                            ? String.Format(CultureInfo.CurrentCulture, "{0}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds)
                            : String.Format(CultureInfo.CurrentCulture, "{0}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
                        return length;
                    }, description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Bitrate:
                    AddTextColumn(t => string.Format("{0} kbps", t.Bitrate), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.BitsPerSample:
                    AddTextColumn(t => t.BitsPerSample.ToString(CultureInfo.InvariantCulture), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Bpm:
                    AddTextColumn(t => t.Bpm.ToString(), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.ChannelCount:
                    AddTextColumn(t => t.ChannelCount.ToString(CultureInfo.InvariantCulture), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Codec:
                    AddTextColumn(t => t.Codec, description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Date:
                    AddTextColumn(t => t.Date, description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.DateAdded:
                    AddTextColumn(t => string.Format("{0} {1}", t.DateAdded.ToShortDateString(), t.DateAdded.ToLongTimeString()), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.DateLastPlayed:
                    AddTextColumn(t => t.DateLastPlayed.HasValue && t.DateLastPlayed.Value > DateTime.MinValue
                        ? string.Format("{0} {1}", t.DateLastPlayed.Value.ToShortDateString(), t.DateLastPlayed.Value.ToLongTimeString()) 
                        : null,
                        description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.DiscNumber:
                    AddTextColumn(t => t.DiscNumber.ToString(), description, sortColumnId, (track1, track2) =>
                        {
                            uint discNumber1 = track1.DiscNumber ?? 0;
                            uint discNumber2 = track2.DiscNumber ?? 0;
                            return discNumber1.CompareTo(discNumber2);
                        }, columnWidth);
                    break;
                case TrackProperty.Genre:
                    AddTextColumn(t => t.Artists == null || t.Artists.Count == 0 ? null : String.Join("; ", t.Genres.Select(g => g.Name)), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.MusicBrainzAlbumId:
                    AddTextColumn(t => t.Album != null ? t.Album.MusicBrainzId : null, description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.MusicBrainzReleaseId:
                    AddTextColumn(t => t.MusicBrainzId, description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Path:
                    AddTextColumn(t => t.Path, description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Playcount:
                    AddTextColumn(t => t.Playcount.ToString(CultureInfo.InvariantCulture), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Rating:
                    // TODO: implement stars
                    AddTextColumn(t => t.Rating.ToString(), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.SampleRate:
                    AddTextColumn(t => string.Format("{0} Hz", t.SampleRate), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Size:
                    AddTextColumn(t => string.Format("{0:N1} MB", (double)t.Size / (1024 * 1024)), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Summary:
                    AddTextColumn(t => string.Format("{0} - {1}", String.Join("; ", t.Artists.Select(a => a.Name)), t.Title), description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.Title:
                    AddTextColumn(t => t.Title, description, sortColumnId, columnWidth);
                    break;
                case TrackProperty.TrackNumber:
                    AddTextColumn(t => t.TrackNumber.ToString(), description, sortColumnId, (track1, track2) =>
                        {
                            var num1 = track1.TrackNumber ?? Int64.MinValue;
                            var num2 = track2.TrackNumber ?? Int64.MinValue;
                            return num1.CompareTo(num2);
                        }, columnWidth);
                    break;
                default:
                    Console.WriteLine("Column not implemented for track property name {0}", description);
                    break;
            }
        }
    }

    class TracksListViewSettings : ApplicationSettingsBase
    {
        private static readonly TrackProperty[] DefaultColumnPropertyArray = { TrackProperty.TrackNumber, TrackProperty.Title, TrackProperty.Artist, TrackProperty.Album, TrackProperty.AlbumArtist, TrackProperty.Date, TrackProperty.Genre, TrackProperty.Rating, TrackProperty.Bitrate, TrackProperty.Size, TrackProperty.DateAdded, TrackProperty.Codec, TrackProperty.Path };

        private static readonly Dictionary<TrackProperty, int> DefaultColumnWidths = new Dictionary<TrackProperty, int>
            {
                {TrackProperty.Album, 200},
                {TrackProperty.AlbumArtist, 200},
                {TrackProperty.Artist, 200},
                {TrackProperty.Bitrate, 80},
                {TrackProperty.BitsPerSample, 30},
                {TrackProperty.Bpm, 50},
                {TrackProperty.ChannelCount, 20},
                {TrackProperty.Codec, 100},
                {TrackProperty.Date, 200},
                {TrackProperty.DateAdded, 200},
                {TrackProperty.DateLastPlayed, 200},
                {TrackProperty.DiscNumber, 20},
                {TrackProperty.Duration, 50},
                {TrackProperty.Genre, 200},
                {TrackProperty.MusicBrainzAlbumId, 200},
                {TrackProperty.MusicBrainzReleaseId, 200},
                {TrackProperty.Path, 200},
                {TrackProperty.Playcount, 30},
                {TrackProperty.Rating, 200},
                {TrackProperty.SampleRate, 50},
                {TrackProperty.Size, 50},
                {TrackProperty.Summary, 200},
                {TrackProperty.Title, 200},
                {TrackProperty.TrackNumber, 30}
            };

        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public TrackProperty[] ColumnPropertyArray
        {
            get
            {
                return (TrackProperty[])this["ColumnPropertyArray"];
            }
            set
            {
                this["ColumnPropertyArray"] = (TrackProperty[])value;
            }
        }

        [UserScopedSetting]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public Dictionary<TrackProperty, int> ColumnWidths
        {
            get
            {
                return (Dictionary<TrackProperty, int>)this["ColumnWidths"];
            }
            set
            {
                this["ColumnWidths"] = (Dictionary<TrackProperty, int>)value;
            }
        }

        public void LoadDefaultColumnPropertyArray()
        {
            ColumnPropertyArray = DefaultColumnPropertyArray;
        }

        public void LoadDefaultColumnWidths()
        {
            ColumnWidths = DefaultColumnWidths;
        }
    }
}
