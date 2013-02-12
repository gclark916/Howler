using System;
using System.Collections.Generic;
using System.Linq;
using Gst;
using Gst.BasePlugins;

namespace Howler.Core.Playback
{
    class AudioPlayer
    {
        private readonly PlayBin2 _playBin;
        private readonly Queue<string> _uriQueue;
        private string _currentSong;

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
            _playBin.AboutToFinish += (o, args) =>
                {
                    _currentSong = _uriQueue.Dequeue();
                    _playBin.SetState(State.Null);
                    _playBin.Uri = _currentSong;
                };

            _uriQueue = new Queue<string>();
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
                    _playBin.SetState(State.Null);
                    if (_uriQueue.Any())
                    {
                        _currentSong = _uriQueue.Dequeue();
                        _playBin.Uri = _currentSong;
                        _playBin.SetState(State.Playing);
                    }
                    else
                    {
                        _currentSong = null;
                    }
                    break;
            }

            return true;
        }

        public void ClearQueue()
        {
            _currentSong = null;
            _uriQueue.Clear();
        }

        public void Enqueue(string[] trackUriArray)
        {
            foreach (string trackUri in trackUriArray)
                _uriQueue.Enqueue(trackUri);
            if (_currentSong == null)
            {
                _currentSong = _uriQueue.Dequeue();
                _playBin.Uri = _currentSong;
            }
        }

        public void Play()
        {
            _playBin.SetState(State.Playing);
        }

        public void Stop()
        {
            _playBin.SetState(State.Null);
        }
    }
}
