using System;
using Gst;
using Gst.BasePlugins;
using Howler.Core.Database;

namespace Howler.Core.Playback
{
    class AudioPlayer
    {
        private readonly PlayBin2 _playBin;
        private Track[] _trackArray;
        private uint _currentTrackIndex;
        public event TrackChangedHandler TrackChanged;

        protected virtual void OnTrackChanged(TrackChangedHandlerArgs args)
        {
            TrackChangedHandler handler = TrackChanged;
            if (handler != null) 
                handler(this, args);
        }

        public AudioPlayer()
        {
            // These environment variables are necessary to locate GStreamer libraries, and to stop it from loading
            // wrong libraries installed elsewhere on the system.
            var directoryName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (directoryName != null)
            {
                string apppath = directoryName.Remove(0, 6);
                string pluginPath = apppath + @"\gstreamer\bin\plugins";
                Environment.SetEnvironmentVariable("GST_PLUGIN_PATH", pluginPath);
                Environment.SetEnvironmentVariable("GST_PLUGIN_SYSTEM_PATH", "");
                string pathVariable = @"C:\Windows;" + apppath + @"\gstreamer\lib;"
                                      + apppath + @"\gstreamer\bin;" + Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", pathVariable);
                Environment.SetEnvironmentVariable("GST_REGISTRY", apppath + @"\gstreamer\bin\registry.bin");

                // These are for saving debug information.
                Environment.SetEnvironmentVariable("GST_DEBUG", "*:3");
                Environment.SetEnvironmentVariable("GST_DEBUG_FILE", "GstreamerLog.txt");
                Environment.SetEnvironmentVariable("GST_DEBUG_DUMP_DOT_DIR", apppath);
            }

            Application.Init();

            _playBin = new PlayBin2("_playBin");
            _playBin.Bus.AddWatch(OnBusMessage);
            /*_playBin.AboutToFinish += (o, args) =>
                {
                    if (_currentTrackIndex + 1 >= _trackArray.Length) 
                        return;

                    OnTrackChanged(new TrackChangedHandlerArgs
                        {
                            NewTrack = _trackArray[_currentTrackIndex + 1],
                            OldTrack = _trackArray[_currentTrackIndex]
                        });
                    _playBin.Uri = PathStringToUri(_trackArray[++_currentTrackIndex].Path);
                };*/
            _trackArray = new Track[0];
        }

        public bool IsPlaying { 
            get
            {
                State currentState, pendingState;
                _playBin.GetState(out currentState, out pendingState, 0);
                return pendingState == State.Playing || (currentState == State.Playing && pendingState == State.VoidPending);
            }  
        }

        private bool OnBusMessage(Bus bus, Message message)
        {
            switch (message.Type)
            {
                case MessageType.Error:
                    Enum err;
                    string msg;
                    message.ParseError(out err, out msg);
                    Console.WriteLine("Gstreamer error: {0}\n{1}", msg, message.Structure.Get("debug"));
                    break;
                case MessageType.Eos:
                    if (_currentTrackIndex+1 < _trackArray.Length)
                    {
                        _playBin.SetState(State.Null);
                        OnTrackChanged(new TrackChangedHandlerArgs
                            {
                                NewTrack = _trackArray[_currentTrackIndex + 1], 
                                OldTrack = _trackArray[_currentTrackIndex]
                            });
                        _playBin.Uri = PathStringToUri(_trackArray[++_currentTrackIndex].Path);
                        _playBin.SetState(State.Playing);
                    }
                    break;
            }

            return true;
        }

        public void ClearQueue()
        {
            _playBin.Uri = null;
            _trackArray = new Track[0];
        }

        public void ReplacePlaylistAndPlay(Track[] trackArray, uint trackIndex)
        {
            if (trackIndex >= trackArray.Length)
                throw new ArgumentException("Index out of bounds", "trackIndex");

            OnTrackChanged(new TrackChangedHandlerArgs
            {
                NewTrack = trackArray[trackIndex],
                OldTrack = _currentTrackIndex < _trackArray.Length ? _trackArray[_currentTrackIndex] : null
            });
            _currentTrackIndex = trackIndex;
            _trackArray = trackArray;
            _playBin.SetState(State.Null);
            _playBin.Uri = PathStringToUri(_trackArray[_currentTrackIndex].Path);
            _playBin.SetState(State.Playing);
        }

        public void Play()
        {
            _playBin.SetState(State.Playing);
        }

        public void Stop()
        {
            _playBin.SetState(State.Ready);
        }

        public void PreviousTrack()
        {
            if (_currentTrackIndex <= 0) 
                return;

            _playBin.SetState(State.Null);
            OnTrackChanged(new TrackChangedHandlerArgs
            {
                NewTrack = _trackArray[_currentTrackIndex - 1],
                OldTrack = _trackArray[_currentTrackIndex]
            });
            _playBin.Uri = PathStringToUri(_trackArray[--_currentTrackIndex].Path);
            _playBin.SetState(State.Playing);
        }

        public void NextTrack()
        {
            if (_currentTrackIndex + 1 < _trackArray.Length)
            {
                _playBin.SetState(State.Null);
                OnTrackChanged(new TrackChangedHandlerArgs
                {
                    NewTrack = _trackArray[_currentTrackIndex + 1],
                    OldTrack = _trackArray[_currentTrackIndex]
                });
                _playBin.Uri = PathStringToUri(_trackArray[++_currentTrackIndex].Path);
                _playBin.SetState(State.Playing);
            }
            else
            {
                _playBin.SetState(State.Ready);
            }
        }

        public void Pause()
        {
            _playBin.SetState(State.Paused);
        }

        private static string PathStringToUri(string path)
        {
            return "file:///" + path;
        }
    }

    internal delegate void TrackChangedHandler(object sender, TrackChangedHandlerArgs args);

    internal class TrackChangedHandlerArgs
    {
        public Track OldTrack;
        public Track NewTrack;
    }
}
