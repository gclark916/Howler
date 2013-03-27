using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using GLib;
using Gtk;
using Howler.Core.MediaLibrary.Entities;
using Howler.Core.Playback;
using Howler.Gui;
using Howler.Util;

namespace Howler.Control
{
    internal abstract class BaseTrackListViewController : ITrackSelector
    {
        protected readonly AudioPlayer AudioPlayer;

        protected readonly Dictionary<TrackListViewColumn, TrackProperty> ColumnsToProperties =
            new Dictionary<TrackListViewColumn, TrackProperty>();

        protected readonly Dictionary<TrackProperty, TrackListViewColumn> PropertiesToColumns =
            new Dictionary<TrackProperty, TrackListViewColumn>();

        protected readonly TrackListView TrackListView = new TrackListView();
        private readonly BaseTrackListViewControllerSettings _settings;

        protected BaseTrackListViewController(ITrackListModel model, BaseTrackListViewControllerSettings settings,
                                              AudioPlayer audioPlayer)
        {
            _settings = settings;
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

            AudioPlayer = audioPlayer;

            TrackListView.Model = model;

            int sortColumnId = 0;
            foreach (TrackProperty property in _settings.ColumnPropertyArray)
            {
                AddColumn(property, sortColumnId++);
            }

            TrackListView.RowActivated += TrackListViewOnRowActivated;
            TrackListView.ColumnsChanged += TrackListViewOnColumnsChanged;
            TrackListView.Selection.Changed += SelectionOnChanged;
            TrackListView.Destroyed += (sender, args) =>
                {
                    Console.WriteLine("Destroyed");
                    TrackListView.ColumnsChanged -= TrackListViewOnColumnsChanged;

                    foreach (var column in ColumnsToProperties.Keys)
                    {
                        column.RemoveNotification("width", TrackListViewColumnNotifyHandler);
                    }
                };
            AudioPlayer.TrackChanged += AudioPlayerOnTrackChanged;

            ((ITrackListModel) TrackListView.Model).DefaultSortFunc = DefaultSortFunc;

            View = new ScrolledWindow {TrackListView};
        }

        public ScrolledWindow View { get; private set; }

        #region ITrackSelector Members

        public Track CurrentTrack
        {
            get { return ((ITrackListModel) TrackListView.Model).CurrentTrack; }
        }

        public bool HasFocus
        {
            get { return TrackListView.HasFocus; }
        }

        public event SelectedTrackHandler SelectedTrack;

        #endregion

        private void SelectionOnChanged(object sender, EventArgs eventArgs)
        {
            ITrackListModel model = TrackListView.Model as ITrackListModel;
            TreePath[] rows = TrackListView.Selection.GetSelectedRows();
            if (!rows.Any())
                return;

            Debug.Assert(model != null, "model != null");

            TreeIter iter;
            model.GetIter(out iter, rows[0]);
            Track track = (Track) model.GetValue(iter, 0);

            var args = new SelectedTrackEventArgs {SelectedTrack = track};
            SelectedTrackHandler handler = SelectedTrack;
            if (handler != null)
                handler(this, args);
        }

        private void TrackListViewOnColumnsChanged(object sender, EventArgs eventArgs)
        {
            TrackProperty[] trackPropertyArray = new TrackProperty[TrackListView.Columns.Count()];
            int trackPropertyIndex = 0;
            foreach (TrackListViewColumn column in TrackListView.Columns)
            {
                TrackProperty trackProperty;
                if (ColumnsToProperties.TryGetValue(column, out trackProperty))
                    trackPropertyArray[trackPropertyIndex++] = trackProperty;
            }

            _settings.ColumnPropertyArray = trackPropertyArray;
        }

        private static int DefaultSortFunc(TreeModel model, TreeIter iter1, TreeIter iter2)
        {
            Track track1 = (Track) model.GetValue(iter1, 0);
            Track track2 = (Track) model.GetValue(iter2, 0);
            int result = string.Compare(string.Join("", track1.Artists), string.Join("", track2.Artists),
                                        StringComparison.CurrentCulture);
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

        protected abstract void TrackListViewOnRowActivated(object o, RowActivatedArgs args);

        private void AudioPlayerOnTrackChanged(object sender, TrackChangedEventArgs args)
        {
            var trackListModel = TrackListView.Model as ITrackListModel;
            Debug.Assert(trackListModel != null, "trackListModel != null");
            trackListModel.HandleTrackChanged(args);
        }

        private void AddTextColumn(StringPropertySelector selector, string columnName, int sortColumnId,
                                   TrackProperty property)
        {
            AddTextColumn(selector, columnName, sortColumnId,
                          (track1, track2) =>
                          string.Compare(selector(track1), selector(track2), StringComparison.CurrentCulture), property);
        }

        private void AddTextColumn(StringPropertySelector selector, string columnName, int sortColumnId,
                                   TrackComparer comparer, TrackProperty property)
        {
            int fixedWidth;
            if (!_settings.ColumnWidths.TryGetValue(property, out fixedWidth))
                fixedWidth = 200;
            TrackListViewColumn genericColumn = new TrackListViewColumn(columnName) {FixedWidth = fixedWidth};
            PropertiesToColumns.Add(property, genericColumn);
            ColumnsToProperties.Add(genericColumn, property);

            TrackCellRenderer pathCellTextRenderer = new TrackCellRenderer();
            genericColumn.PackStart(pathCellTextRenderer, true);
            genericColumn.SetCellDataFunc(pathCellTextRenderer,
                                          (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter) =>
                                              {
                                                  Track track = (Track) model.GetValue(iter, 0);
                                                  Track playingTrack = ((ITrackListModel) model).CurrentTrack;
                                                  bool playing = playingTrack != null && track.Equals(playingTrack);
                                                  ((CellRendererText) cell).Text = selector(track);
                                                  ((CellRendererText) cell).Weight = playing ? 800 : 400;
                                              });

            if (sortColumnId >= 0)
            {
                genericColumn.SortColumnId = sortColumnId;
                ((ITrackListModel) TrackListView.Model).SetSortFunc(sortColumnId, (model, iter1, iter2) =>
                    {
                        Track track1 = (Track) model.GetValue(iter1, 0);
                        Track track2 = (Track) model.GetValue(iter2, 0);
                        int result = comparer(track1, track2);
                        if (result == 0)
                            result = DefaultSortFunc(model, iter1, iter2);
                        return result;
                    });
            }

            TrackListView.AppendColumn(genericColumn);
            genericColumn.AddNotification("width", TrackListViewColumnNotifyHandler);
        }

        private void TrackListViewColumnNotifyHandler(object o, NotifyArgs args)
        {
            TrackProperty property;
            TrackListViewColumn column = o as TrackListViewColumn;
            if (!ColumnsToProperties.TryGetValue((TrackListViewColumn) o, out property))
                return;

            int width;
            if (!_settings.ColumnWidths.TryGetValue(property, out width))
                return;

            if (column == null || width == column.Width || column.Width <= 5)
                return;

            _settings.ColumnWidths.Remove(property);
            _settings.ColumnWidths.Add(property, column.Width);
            _settings.Save();
        }

        private void AddColumn(TrackProperty property, int sortColumnId)
        {
            string description = Extensions.GetEnumDescription(property);
            switch (property)
            {
                case TrackProperty.Album:
                    AddTextColumn(t => t.Album != null ? t.Album.Title : null, description, sortColumnId, property);
                    break;
                case TrackProperty.Artist:
                    AddTextColumn(
                        t =>
                        t.Artists == null || t.Artists.Count == 0
                            ? null
                            : String.Join("; ", t.Artists.Select(a => a.Name)), description, sortColumnId, property);
                    break;
                case TrackProperty.AlbumArtist:
                    AddTextColumn(t => (t.Album == null || t.Album.Artists == null || t.Artists.Count == 0)
                                           ? null
                                           : String.Join("; ", t.Album.Artists.Select(a => a.Name)), description,
                                  sortColumnId, property);
                    break;
                case TrackProperty.Duration:
                    AddTextColumn(t =>
                        {
                            TimeSpan timeSpan = TimeSpan.FromMilliseconds(t.Duration);
                            string length = t.Duration >= 1000*60*60
                                                ? String.Format(CultureInfo.CurrentCulture, "{0}:{1:D2}:{2:D2}",
                                                                timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds)
                                                : String.Format(CultureInfo.CurrentCulture, "{0}:{1:D2}",
                                                                timeSpan.Minutes, timeSpan.Seconds);
                            return length;
                        }, description, sortColumnId, property);
                    break;
                case TrackProperty.Bitrate:
                    AddTextColumn(t => t.Bitrate.ToString(CultureInfo.InvariantCulture), description, sortColumnId,
                                  (track1, track2) => track1.Bitrate.CompareTo(track2.Bitrate), property);
                    break;
                case TrackProperty.BitsPerSample:
                    AddTextColumn(t => t.BitsPerSample.ToString(CultureInfo.InvariantCulture), description, sortColumnId,
                                  (track1, track2) => track1.BitsPerSample.CompareTo(track2.BitsPerSample), property);
                    break;
                case TrackProperty.Bpm:
                    AddTextColumn(t => t.Bpm.ToString(), description, sortColumnId, (track1, track2) =>
                        {
                            uint bpm1 = track1.Bpm ?? 0;
                            uint bpm2 = track2.Bpm ?? 0;
                            return bpm1.CompareTo(bpm2);
                        }, property);
                    break;
                case TrackProperty.ChannelCount:
                    AddTextColumn(t => t.ChannelCount.ToString(CultureInfo.InvariantCulture), description, sortColumnId,
                                  (track1, track2) => track1.ChannelCount.CompareTo(track2.ChannelCount), property);
                    break;
                case TrackProperty.Codec:
                    AddTextColumn(t => t.Codec, description, sortColumnId, property);
                    break;
                case TrackProperty.Date:
                    AddTextColumn(t => t.Date, description, sortColumnId, property);
                    break;
                case TrackProperty.DateAdded:
                    AddTextColumn(
                        t => string.Format("{0} {1}", t.DateAdded.ToShortDateString(), t.DateAdded.ToLongTimeString()),
                        description, sortColumnId, (track1, track2) => track1.DateAdded.CompareTo(track2.DateAdded),
                        property);
                    break;
                case TrackProperty.DateLastPlayed:
                    AddTextColumn(t => t.DateLastPlayed.HasValue && t.DateLastPlayed.Value > DateTime.MinValue
                                           ? string.Format("{0} {1}", t.DateLastPlayed.Value.ToShortDateString(),
                                                           t.DateLastPlayed.Value.ToLongTimeString())
                                           : null,
                                  description, sortColumnId, (track1, track2) =>
                                      {
                                          DateTime date1 = track1.DateLastPlayed ?? DateTime.MinValue;
                                          DateTime date2 = track2.DateLastPlayed ?? DateTime.MinValue;
                                          return date1.CompareTo(date2);
                                      }, property);
                    break;
                case TrackProperty.DiscNumber:
                    AddTextColumn(t => t.DiscNumber.ToString(), description, sortColumnId, (track1, track2) =>
                        {
                            uint discNumber1 = track1.DiscNumber ?? 0;
                            uint discNumber2 = track2.DiscNumber ?? 0;
                            return discNumber1.CompareTo(discNumber2);
                        }, property);
                    break;
                case TrackProperty.Genre:
                    AddTextColumn(
                        t =>
                        t.Artists == null || t.Artists.Count == 0
                            ? null
                            : String.Join("; ", t.Genres.Select(g => g.Name)), description, sortColumnId, property);
                    break;
                case TrackProperty.MusicBrainzAlbumId:
                    AddTextColumn(t => t.Album != null ? t.Album.MusicBrainzId : null, description, sortColumnId,
                                  property);
                    break;
                case TrackProperty.MusicBrainzReleaseId:
                    AddTextColumn(t => t.MusicBrainzId, description, sortColumnId, property);
                    break;
                case TrackProperty.Path:
                    AddTextColumn(t => t.Path, description, sortColumnId, property);
                    break;
                case TrackProperty.Playcount:
                    AddTextColumn(t => t.Playcount.ToString(CultureInfo.InvariantCulture), description, sortColumnId,
                                  (track1, track2) => track1.Playcount.CompareTo(track2.Playcount), property);
                    break;
                case TrackProperty.Rating:
                    // TODO: implement stars
                    AddTextColumn(t => t.Rating.ToString(), description, sortColumnId, (track1, track2) =>
                        {
                            uint rating1 = track1.Rating ?? 0;
                            uint rating2 = track2.Rating ?? 0;
                            return rating1.CompareTo(rating2);
                        }, property);
                    break;
                case TrackProperty.SampleRate:
                    AddTextColumn(t => string.Format("{0} Hz", t.SampleRate), description, sortColumnId,
                                  (track1, track2) => track1.SampleRate.CompareTo(track2.SampleRate), property);
                    break;
                case TrackProperty.Size:
                    AddTextColumn(t => string.Format("{0:N1} MB", (double) t.Size/(1024*1024)), description,
                                  sortColumnId, (track1, track2) => track1.Size.CompareTo(track2.Size), property);
                    break;
                case TrackProperty.Summary:
                    AddTextColumn(
                        t => string.Format("{0} - {1}", String.Join("; ", t.Artists.Select(a => a.Name)), t.Title),
                        description, sortColumnId, property);
                    break;
                case TrackProperty.Title:
                    AddTextColumn(t => t.Title, description, sortColumnId, property);
                    break;
                case TrackProperty.TrackNumber:
                    AddTextColumn(t => t.TrackNumber.ToString(), description, sortColumnId, (track1, track2) =>
                        {
                            uint num1 = track1.TrackNumber ?? uint.MinValue;
                            uint num2 = track2.TrackNumber ?? uint.MinValue;
                            return num1.CompareTo(num2);
                        }, property);
                    break;
                default:
                    Console.WriteLine("Column not implemented for track property name {0}", description);
                    break;
            }
        }

        #region Nested type: StringPropertySelector

        private delegate string StringPropertySelector(Track track);

        #endregion

        #region Nested type: TrackComparer

        private delegate int TrackComparer(Track track1, Track track2);

        #endregion
    }
}