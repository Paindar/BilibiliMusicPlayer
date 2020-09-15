using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BilibiliMusicPlayer
{
    public enum PlayState { PLAYING, PAUSE, STOP, SWITCH};
    enum PlayMode { SONG_LOOP, RANDOM_PLAY, ORDER_PLAY, LIST_LOOP}//单曲循环，随机播放，顺序播放，列表循环
    class MusicPlayer
    {
        public delegate void MusicEvent(SongInfo info);
        public class SongInfo
        {
            public string LocalPath = "";
            public string Url = "";
            public string Name = "";
            public string Id = "";
            public SongInfo() { }

            public SongInfo(SongInfo info)
            {
                this.LocalPath = info.LocalPath;
                this.Url = info.Url;
                this.Name = info.Name;
                this.Id = info.Id;
            }

            public JObject ToJson()
            {
                JObject obj = new JObject();
                obj["localpath"] = LocalPath;
                obj["url"] = Url;
                obj["name"] = Name;
                obj["id"] = Id;
                return obj;
            }

            public static SongInfo FromJson(JObject obj)
            {
                SongInfo info = new SongInfo();
                info.LocalPath = obj["localpath"].ToString();
                info.Url = obj["url"].ToString();
                info.Name = obj["name"].ToString();
                info.Id = obj["id"].ToString();
                return info;
            }
        }
        public class PlayStatus
        {
            public SongInfo info;
            public int idx;
            public PlayState state;
            public PlayMode mode;
            public TimeSpan curTime;
            public TimeSpan totalTime;
        }
        public class URLUnrecognizedException : Exception { public string URL; }
        private static MusicEvent OnMusicStart;
        private static MusicEvent OnMusicPause;
        private static MusicEvent OnMusicEnd;
        public event MusicEvent MusicStart
        {
            add { OnMusicStart += new MusicEvent(value); }
            remove { OnMusicStart += new MusicEvent(value); }
        }
        public event MusicEvent MusicPause
        {
            add { OnMusicPause += new MusicEvent(value); }
            remove { OnMusicPause += new MusicEvent(value); }
        }
        public event MusicEvent MusicEnd
        {
            add { OnMusicEnd += new MusicEvent(value); }
            remove { OnMusicEnd += new MusicEvent(value); }
        }


        private AudioFileReader audioFile = null;
        private WaveOutEvent outputDevice = new WaveOutEvent();
        private object resLocker = new object();
        private int curIdx = 0;
        private List<SongInfo> playlist = new List<SongInfo>();
        private PlayState playState = PlayState.STOP;
        private PlayMode playMode = PlayMode.ORDER_PLAY;

        private DirectoryInfo cacheDir;
        private string savePath;
        private Thread thread;
        private MediaGetter mdGetter = new MediaGetter();
        public MusicPlayer(string cache="./cache", string savePath="./config.json")
        {
            cacheDir = new DirectoryInfo(cache);
            if (cacheDir.Exists == false)
                cacheDir.Create();
            CreateNewThread();
            this.savePath = savePath;
            
        }
        private void CreateNewThread()
        {
            thread = new Thread(new ThreadStart(() => {
                while (true)
                {
                    _play();
                    bool exit = false;
                    lock (resLocker)
                    {
                        exit = (playState == PlayState.STOP);
                    }
                    if (exit) break;
                }
            }))
            {
                IsBackground = true
            };
        }
        public SongInfo AddSong(string url, string name = "")
        {
            SongInfo info;
            if (url.StartsWith("file://"))
            {
                info = new SongInfo { LocalPath = url, Name = name, Url = "" };
            }
            else if (BVConvert.isAV(url))
            {
                info = new SongInfo();
                string avid = BVConvert.video_trav(url);
                info.Id = avid;
                info.Url = url;
                info.Name = (name == "") ? avid : name;
                mdGetter.GetAudioTitle(avid).ContinueWith(s => {
                    if (s.Result != "")
                        info.Name = s.Result;
                    else
                        info.Name = (name == "") ? avid : name;
                });
            }
            else if (BVConvert.isBV(url))
            {
                info = new SongInfo();
                string bvid = BVConvert.video_trbv(url);
                info.Id = bvid;
                info.Url = url;
                mdGetter.GetAudioTitle(bvid).ContinueWith(s => {
                    if (s.Result != "")
                        info.Name = s.Result;
                    else
                        info.Name = (name == "") ? bvid : name;
                });
            }
            else
                throw new URLUnrecognizedException()
                { 
                    URL = url 
                };
            lock(resLocker)
            {
                playlist.Add(info);
            }
            return new SongInfo(info);
        }
        public SongInfo Remove(int idx)
        {
            SongInfo info=null;
            lock(resLocker)
            {
                if(idx>=0 && idx <playlist.Count)
                {
                    info = playlist[idx];
                    playlist.RemoveAt(idx);
                }
            }
            return info;
        }
        public void Clear()
        {
            lock(resLocker){
                playState = PlayState.STOP;
                playlist.Clear();
                curIdx = 0;
            }
        }
        public bool TryLocalFile(string name, out string localFile)
        {
            FileInfo[] infos = cacheDir.GetFiles();
            foreach(var i in infos)
            {
                if (i.Name.StartsWith(name))
                { 
                    localFile = i.FullName;
                    return true;
                }
            }
            localFile = "";
            return false;
        }
        private void _play()
        {
            string localFile="";
            SongInfo copy;
            lock (resLocker){
                if (playlist.Count == 0)
                    throw new Exception("Playlist is empty!");

                if (curIdx >= playlist.Count)
                { 
                    curIdx %= playlist.Count;
                }
                SongInfo info = playlist[curIdx];
                copy = new SongInfo(info);
                if(info.LocalPath.Length==0)
                {
                    if(TryLocalFile(info.Id, out localFile))
                    {
                        info.LocalPath = localFile;
                    }
                }
                else
                {
                    localFile = info.LocalPath;
                }
            }
            string id = copy.Id;
            if (localFile.Length==0)
            {
                if (id.StartsWith("av"))
                {
                    string musicname = cacheDir.FullName + "/" + id + "_Audio.m4a";
                    mdGetter.DownloadAV(id, musicname).Wait();
                    localFile = musicname;
                }
                else if (id.StartsWith("BV"))
                {
                    string musicname = cacheDir.FullName + "/" + id + "_Audio.m4a";
                    mdGetter.DownloadBV(id, musicname).Wait();
                    localFile = musicname;
                }
            }

            audioFile = new AudioFileReader(localFile);
            outputDevice.Init(audioFile);
            outputDevice.Play();
            playState = PlayState.PLAYING;
            OnMusicStart?.Invoke(copy);
            while (true)
            {
                lock (resLocker)
                {
                    if (playState == PlayState.SWITCH)
                    {
                        outputDevice.Stop();
                        audioFile.Close();
                        return;
                    }
                }
                switch (outputDevice.PlaybackState)
                {
                    case PlaybackState.Playing:
                        lock (resLocker)
                        {
                            if (playState == PlayState.PAUSE)
                            {
                                outputDevice.Pause();
                                OnMusicPause?.Invoke(copy);
                            }
                            else if (playState == PlayState.STOP)//User let player stop, quit player and reset idx.
                            {
                                outputDevice.Stop();
                                audioFile.Close();
                                curIdx = 0;
                                return;
                            }
                        }
                        break;
                    case PlaybackState.Stopped://Player stop, change idx due to playMode
                        OnMusicEnd?.Invoke(copy);
                        //No need to switch(playState), player stop isn't a continuous state.
                        lock (resLocker)
                        {
                            switch (playMode)
                            {
                                case PlayMode.LIST_LOOP:
                                    curIdx++;
                                    break;
                                case PlayMode.ORDER_PLAY:
                                    curIdx++;
                                    if (curIdx >= playlist.Count)
                                        playState = PlayState.STOP;
                                    break;
                                case PlayMode.RANDOM_PLAY:
                                    if (playlist.Count != 1)
                                    {
                                        int rand = Utils.NextInt(playlist.Count - 1);
                                        if (rand == curIdx) rand++;
                                        curIdx = rand;
                                    }
                                    break;
                                case PlayMode.SONG_LOOP:
                                    break;
                            }
                        }
                        audioFile.Close();
                        return;//end this loop.
                    case PlaybackState.Paused:
                        lock (resLocker)
                        {
                            if (playState == PlayState.PLAYING)
                            {
                                outputDevice.Play();
                            }
                        }
                        break;
                }
                Thread.Sleep(200);
            }
        }
        public void Play()
        {
            if (thread.IsAlive)
            {
                lock(resLocker)
                {
                    if(playState == PlayState.PAUSE)
                    {
                        playState = PlayState.PLAYING;
                    }
                }
            }
            else
            {
                CreateNewThread();
                thread.Start();
            }
        }
        public void Pause()
        {
            lock(resLocker)
            {
                if (playState == PlayState.PLAYING)
                    playState = PlayState.PAUSE;
                else if (playState == PlayState.PAUSE)
                    playState = PlayState.PLAYING;
            }
        }
        public void Stop()
        {
            lock(resLocker)
            {
                playState = PlayState.STOP;
            }
        }
        public PlayStatus GetStatus()
        {
            PlayStatus ret = new PlayStatus();
            lock(resLocker)
            {
                ret.idx = curIdx;
                ret.info = null;
                if (curIdx >= 0 && curIdx <= playlist.Count)
                    ret.info = new SongInfo(playlist[curIdx]);
                ret.mode = playMode;
                ret.state = playState;
                if (audioFile != null)
                {
                    ret.curTime = audioFile.CurrentTime;
                    ret.totalTime = audioFile.TotalTime;
                }
            }
            return ret;
        }
        public void SetPlayMode(PlayMode mode)
        {
            lock(resLocker)
            {
                playMode = mode;
            }
        }
        public void Next()
        {
            lock (resLocker)
            {
                curIdx++;
                playState = PlayState.SWITCH;
            }
        }
        public void Last()
        {
            lock (resLocker)
            {
                curIdx--;
                playState = PlayState.SWITCH;
            }
        }

        public void Select(int idx)
        {
            lock (resLocker)
            {
                curIdx=idx;
                playState = PlayState.SWITCH;
            }
        }

        public IList<SongInfo> List { get { return playlist.AsReadOnly(); } }
        public void Save()
        {
            JObject js = new JObject();
            lock(resLocker)
            {
                js["playmode"] = (int)playMode;
                //js["playstate"] = (int)playState;
                JArray array = new JArray(playlist.Select(p => new JObject
                {
                    { "localpath", p.LocalPath },
                    { "url", p.Url},
                    { "name", p.Name},
                    {"id", p.Id }
                })
                );
                js["playlist"] = array;
            }
            using (StreamWriter sw = new StreamWriter(savePath))
            {
                sw.Write(js.ToString());
            }
        }
        public void Load()
        {
            JObject js;
            var serializer = new JsonSerializer();
            if (File.Exists(savePath) == false)
                return;
            using (StreamReader sr = new StreamReader(savePath))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    try
                    {
                        js = serializer.Deserialize<JObject>(reader);
                    }
                    catch(JsonSerializationException)
                    {
                        return;
                    }
                }
            }
            if (js.ContainsKey("header"))
            {
                mdGetter.SetUserAgent(js["header"].ToString());
            }
            lock(resLocker)
            {
                try
                {
                    playMode = (PlayMode)js["playmode"].Value<int>(); 
                    //playState = (PlayState)js["playstate"].Value<int>();
                    JArray arr = js.Value<JArray>("playlist");
                    playlist = arr.Select(p => new SongInfo
                    {
                        LocalPath = p["localpath"].ToString(), 
                        Url = p["url"].ToString(),
                        Name = p["name"].ToString(),
                        Id = p["id"].ToString()
                    }).ToList();
                }
                catch(Exception)
                {
                }
            }
            
        }
    }
}
