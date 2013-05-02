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

        protected readonly TrackListHeaderMenuController HeaderMenuController;

        protected readonly Dictionary<TrackProperty, TrackListViewColumn> PropertiesToColumns =
            new Dictionary<TrackProperty, TrackListViewColumn>();

        protected readonly TrackListView TrackListView = new TrackListView();
        private readonly BaseTrackListViewControllerSettings _settings;

        protected BaseTrackListViewController(ITrackListModel model, BaseTrackListViewControllerSettings settings,
                                              AudioPlayer audioPlayer)
        {
            _settings = settings;
            _settings.Reload();
            if (_settings.ColumnPropertyList == null || !_settings.ColumnPropertyList.Any())
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

            foreach (TrackProperty property in _settings.ColumnPropertyList)
            {
                AddColumn(property);
            }

            TrackListView.RowActivated += TrackListViewOnRowActivated;
            TrackListView.ColumnsChanged += TrackListViewOnColumnsChanged;
            TrackListView.Selection.Changed += SelectionOnChanged;
            TrackListView.Destroyed += (sender, args) =>
                {
                    TrackListView.ColumnsChanged -= TrackListViewOnColumnsChanged;

                    foreach (var column in ColumnsToProperties.Keys)
                    {
                        column.RemoveNotification("width", TrackListViewColumnNotifyHandler);
                    }
                };
            AudioPlayer.TrackChanged += AudioPlayerOnTrackChanged;

            ((ITrackListModel) TrackListView.Model).DefaultSortFunc = DefaultSortFunc;

            View = new ScrolledWindow {TrackListView};
            HeaderMenuController = new TrackListHeaderMenuController(this);
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

        public IEnumerable<TrackProperty> GetColumnTrackProperties()
        {
            return _settings.ColumnPropertyList;
        }

        public void InsertColumn(TrackProperty propertyToInsert, TrackProperty? priorColumnProperty)
        {
            _settings.AddColumn(propertyToInsert);
            _settings.InsertColumn(propertyToInsert, priorColumnProperty);
            AddColumn(propertyToInsert, priorColumnProperty);
        }

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
            List<TrackProperty> trackPropertyList = new List<TrackProperty>(TrackListView.Columns.Count());
            foreach (TrackListViewColumn column in TrackListView.Columns)
            {
                TrackProperty trackProperty;
                if (ColumnsToProperties.TryGetValue(column, out trackProperty))
                    trackPropertyList.Add(trackProperty);
            }

            _settings.ColumnPropertyList = trackPropertyList;
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

        private void AddTextColumn(StringPropertySelector selector, string columnName, TrackComparer comparer,
                                   TrackProperty property, TrackProperty? priorColumnProperty)
        {
            if (comparer == null)
            {
                comparer = (track1, track2) => string.Compare(selector(track1), selector(track2),
                                                              StringComparison.CurrentCulture);
            }
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

            genericColumn.SortColumnId = (int) property;
            ((ITrackListModel) TrackListView.Model).SetSortFunc((int) property, (model, iter1, iter2) =>
                {
                    Track track1 = (Track) model.GetValue(iter1, 0);
                    Track track2 = (Track) model.GetValue(iter2, 0);
                    int result = comparer(track1, track2);
                    if (result == 0)
                        result = DefaultSortFunc(model, iter1, iter2);
                    return result;
                });

            int position = 0;
            foreach (var column in TrackListView.Columns)
            {
                position++;
                TrackProperty columnProperty;
                if (ColumnsToProperties.TryGetValue((TrackListViewColumn) column, out columnProperty))
                {
                    if (columnProperty == priorColumnProperty)
                        break;
                }
            }

            TrackListView.InsertColumn(genericColumn, position);
            genericColumn.AddNotification("width", TrackListViewColumnNotifyHandler);
            var label = new Label(columnName);
            genericColumn.Widget = label;
            var widget = genericColumn.Widget;
            widget.GetAncestor(Button.GType);

            var columnHeader = widget.GetAncestor(Button.GType);
            columnHeader.ButtonPressEvent += ColumnHeaderOnButtonPressEvent;
            label.Show();
        }

        [ConnectBefore()]
        private void ColumnHeaderOnButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            Button button = o as Button;
            Debug.Assert(button != null, "button != null");
            if (args.Event.Button == 3)
            {
                TreePath path;
                TreeViewColumn column;
                TrackListView.GetPathAtPos((int)args.Event.X + button.Allocation.X, (int)args.Event.Y, out path, out column);
                TrackProperty property;
                TrackProperty? nullableProperty = null;
                if (ColumnsToProperties.TryGetValue((TrackListViewColumn) column, out property))
                    nullableProperty = property;
                HeaderMenuController.ClickedProperty = nullableProperty;
                HeaderMenuController.View.Popup();
                Console.WriteLine(o.ToString());
            }
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

        private void AddColumn(TrackProperty property, TrackProperty? priorColumnProperty = null)
        {
            string description = Extensions.GetEnumDescription(property);
            switch (property)
            {
                case TrackProperty.Album:
                    AddTextColumn(t => t.Album != null ? t.Album.Title : null, description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.Artist:
                    AddTextColumn(
                        t =>
                        t.Artists == null || t.Artists.Count == 0
                            ? null
                            : String.Join("; ", t.Artists.Select(a => a.Name)), description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.AlbumArtist:
                    AddTextColumn(t => (t.Album == null || t.Album.Artists == null || t.Artists.Count == 0)
                                           ? null
                                           : String.Join("; ", t.Album.Artists.Select(a => a.Name)), description, null,
                                  property, priorColumnProperty);
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
                        }, description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.Bitrate:
                    AddTextColumn(t => t.Bitrate.ToString(CultureInfo.InvariantCulture), description,
                                  (track1, track2) => track1.Bitrate.CompareTo(track2.Bitrate), property, priorColumnProperty);
                    break;
                case TrackProperty.BitsPerSample:
                    AddTextColumn(t => t.BitsPerSample.ToString(CultureInfo.InvariantCulture), description,
                                  (track1, track2) => track1.BitsPerSample.CompareTo(track2.BitsPerSample), property, priorColumnProperty);
                    break;
                case TrackProperty.Bpm:
                    AddTextColumn(t => t.Bpm.ToString(), description, (track1, track2) =>
                        {
                            uint bpm1 = track1.Bpm ?? 0;
                            uint bpm2 = track2.Bpm ?? 0;
                            return bpm1.CompareTo(bpm2);
                        }, property, priorColumnProperty);
                    break;
                case TrackProperty.ChannelCount:
                    AddTextColumn(t => t.ChannelCount.ToString(CultureInfo.InvariantCulture), description,
                                  (track1, track2) => track1.ChannelCount.CompareTo(track2.ChannelCount), property, priorColumnProperty);
                    break;
                case TrackProperty.Codec:
                    AddTextColumn(t => t.Codec, description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.Date:
                    AddTextColumn(t => t.Date, description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.DateAdded:
                    AddTextColumn(
                        t => string.Format("{0} {1}", t.DateAdded.ToShortDateString(), t.DateAdded.ToLongTimeString()),
                        description, (track1, track2) => track1.DateAdded.CompareTo(track2.DateAdded),
                        property, priorColumnProperty);
                    break;
                case TrackProperty.DateLastPlayed:
                    AddTextColumn(t => t.DateLastPlayed.HasValue && t.DateLastPlayed.Value > DateTime.MinValue
                                           ? string.Format("{0} {1}", t.DateLastPlayed.Value.ToShortDateString(),
                                                           t.DateLastPlayed.Value.ToLongTimeString())
                                           : null,
                                  description, (track1, track2) =>
                                      {
                                          DateTime date1 = track1.DateLastPlayed ?? DateTime.MinValue;
                                          DateTime date2 = track2.DateLastPlayed ?? DateTime.MinValue;
                                          return date1.CompareTo(date2);
                                      }, property, priorColumnProperty);
                    break;
                case TrackProperty.DiscNumber:
                    AddTextColumn(t => t.DiscNumber.ToString(), description, (track1, track2) =>
                        {
                            uint discNumber1 = track1.DiscNumber ?? 0;
                            uint discNumber2 = track2.DiscNumber ?? 0;
                            return discNumber1.CompareTo(discNumber2);
                        }, property, priorColumnProperty);
                    break;
                case TrackProperty.Genre:
                    AddTextColumn(
                        t =>
                        t.Artists == null || t.Artists.Count == 0
                            ? null
                            : String.Join("; ", t.Genres.Select(g => g.Name)), description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.MusicBrainzAlbumId:
                    AddTextColumn(t => t.Album != null ? t.Album.MusicBrainzId : null, description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.MusicBrainzReleaseId:
                    AddTextColumn(t => t.MusicBrainzId, description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.Path:
                    AddTextColumn(t => t.Path, description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.Playcount:
                    AddTextColumn(t => t.Playcount.ToString(CultureInfo.InvariantCulture), description,
                                  (track1, track2) => track1.Playcount.CompareTo(track2.Playcount), property, priorColumnProperty);
                    break;
                case TrackProperty.Rating:
                    // TODO: implement stars
                    AddTextColumn(t => t.Rating.ToString(), description, (track1, track2) =>
                        {
                            uint rating1 = track1.Rating ?? 0;
                            uint rating2 = track2.Rating ?? 0;
                            return rating1.CompareTo(rating2);
                        }, property, priorColumnProperty);
                    break;
                case TrackProperty.SampleRate:
                    AddTextColumn(t => string.Format("{0} Hz", t.SampleRate), description,
                                  (track1, track2) => track1.SampleRate.CompareTo(track2.SampleRate), property, priorColumnProperty);
                    break;
                case TrackProperty.Size:
                    AddTextColumn(t => string.Format("{0:N1} MB", (double) t.Size/(1024*1024)), description,
                                  (track1, track2) => track1.Size.CompareTo(track2.Size), property, priorColumnProperty);
                    break;
                case TrackProperty.Summary:
                    AddTextColumn(
                        t => string.Format("{0} - {1}", String.Join("; ", t.Artists.Select(a => a.Name)), t.Title),
                        description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.Title:
                    AddTextColumn(t => t.Title, description, null, property, priorColumnProperty);
                    break;
                case TrackProperty.TrackNumber:
                    AddTextColumn(t => t.TrackNumber.ToString(), description, (track1, track2) =>
                        {
                            uint num1 = track1.TrackNumber ?? uint.MinValue;
                            uint num2 = track2.TrackNumber ?? uint.MinValue;
                            return num1.CompareTo(num2);
                        }, property, priorColumnProperty);
                    break;
                default:
                    Console.WriteLine("Column not implemented for track property name {0}", description);
                    break;
            }
        }

        public void RemoveColumn(TrackProperty property)
        {
            _settings.RemoveColumn(property);

            TrackListViewColumn column;
            if (PropertiesToColumns.TryGetValue(property, out column))
            {
                TrackListView.RemoveColumn(column);
                ColumnsToProperties.Remove(column);
                PropertiesToColumns.Remove(property);
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