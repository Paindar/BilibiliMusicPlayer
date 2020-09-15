using System;
using System.Collections.Generic;

namespace BilibiliMusicPlayer
{
    class Program
    {
        static void Print(params string[] args)
        {
            Console.WriteLine(string.Join(' ', args));
        }
        static void Main(string[] _args)
        {
            MusicPlayer player = new MusicPlayer();
            int MAX_LEN = 50;
            player.Load();
            //string url1 = "https://www.bilibili.com/video/BV11f4y1X7Qe";
            //string url2 = "https://www.bilibili.com/video/BV1HZ4y1M7DH";
            //player.AddSong(url1);
            //player.AddSong(url2);
            player.MusicStart+=(MusicPlayer.SongInfo info) => { Console.WriteLine("Now playing " + info.Name); };
            Print("Welcome to Bilibili Music Player. Type \"help\" to get more help.");
            while (true)
            {
                string input = Console.ReadLine();
                if (input.Length == 0)
                    break;
                string[] args = input.Split(' ');
                args[0] = args[0].ToLower();
                if (args[0]=="help")
                {
                    Print("Command:");
                    Print("play\n\tPlay music.");
                    Print("pause\n\tPause playing");
                    Print("status\n\tShow player status");
                    Print("list\n\tShow all songs.");
                    Print("add [av, bv, url,file path] <name>\n\tAdd a song (with name).");
                    Print("remove [id]\n\tRemove the song, if id out of range, it will be ignored.");
                    Print("clear\n\tClear playing list.");
                    Print("last\n\tCut to last song.");
                    Print("select [song's id]\n\tCut to selected song.");
                    Print("next\n\tCut to next song.");
                    Print("mode [loop, listloop, order, random]\n\tchange playing mode.");
                    Print("stop\n\tStop playing.Next playing will start with the first song.");
                    Print("exit\n\tGoodbye.");
                }
                else if (args[0] == "play")
                {
                    player.Play();
                }
                else if (args[0]=="pause")
                {
                    player.Pause();
                }
                else if (args[0] == "status")
                {
                    MusicPlayer.PlayStatus status = player.GetStatus();
                    Print(string.Format("Now playing: [{0}] {1}", status.idx, status.info.Name));
                    if(status.totalTime != TimeSpan.Zero)
                    {
                        int per = (int)(status.curTime / status.totalTime * MAX_LEN);
                        Print(string.Format("[{0}{1}{2}] [{3}/{4}]", 
                            new string('=', per), status.state== PlayState.PLAYING?">":"|", new string('-', MAX_LEN - per), 
                            status.curTime.ToString(@"mm\:ss"), status.totalTime.ToString(@"mm\:ss")));
                    }
                    else
                    {
                        Print(string.Format("[|{0}] [0:00/0:00]",new string('-', MAX_LEN)));
                    }
                    string mode = status.mode switch
                    {
                        PlayMode.LIST_LOOP => "list loop",
                        PlayMode.ORDER_PLAY => "order playing",
                        PlayMode.RANDOM_PLAY => "random playing",
                        PlayMode.SONG_LOOP => "song loop",
                        _ => "unknown",
                    };
                    string state = status.state switch
                    {
                        PlayState.PLAYING => "playing",
                        PlayState.PAUSE => "pause",
                        PlayState.STOP => "stopped",
                        _ => "unknown",
                    };
                    Print(string.Format("Playing mode: {0}\t\tPlayer state: {1}", mode, state));
                }
                else if (args[0] == "list")
                {
                    IList<MusicPlayer.SongInfo> lists = player.List;
                    int i = 0;
                    foreach(var info in lists)
                    {
                        Print(string.Format("[{0}]{1}", i, info.Name));
                        i++;
                    }
                }
                else if (args[0]=="add")
                {
                    try
                    {
                        player.AddSong(args[1], args.Length==3?args[2]:"");
                    }
                    catch(MusicPlayer.URLUnrecognizedException e)
                    {
                        Print("Cannot recognize url/file path: " + e.URL);
                    }
                }
                else if (args[0]=="remove")
                {
                    if (int.TryParse(args[1], out int idx))
                        player.Remove(idx);
                    else
                    {
                        Print(string.Format("Cannot recognize {0}, it should be a integer", args[1]));
                    }
                }
                else if (args[0]=="clear")
                {
                    player.Clear();
                }
                else if (args[0] == "last")
                {
                    player.Last();
                }
                else if (args[0] == "select")
                {
                    if (int.TryParse(args[1], out int idx))
                        player.Select(idx);
                    else
                    {
                        Print(string.Format("Cannot recognize {0}, it should be a integer", args[1]));
                    }
                }
                else if (args[0] == "next")
                {
                    player.Next();
                }
                else if (args[0]=="mode")
                {
                    if (args[1] == "loop")
                    {
                        player.SetPlayMode(PlayMode.SONG_LOOP);
                        Print("Set playing mode: single loop.");
                    }
                    else if(args[1] == "listloop")
                    {
                        player.SetPlayMode(PlayMode.LIST_LOOP);
                        Print("Set playing mode: list loop.");
                    }
                    else if (args[1] == "order")
                    {
                        player.SetPlayMode(PlayMode.ORDER_PLAY);
                        Print("Set playing mode: order playing.");
                    }
                    else if (args[1] == "random")
                    {
                        player.SetPlayMode(PlayMode.RANDOM_PLAY);
                        Print("Set playing mode: randomly playing.");
                    }
                    else
                    {
                        Print("Cannot recognize playing mode: " + args[1]);
                        Print("Allowed mode: mode [loop, listloop, order, random]");
                    }
                }
                else if (args[0]=="stop")
                {
                    player.Stop();
                }
                else if (args[0] == "exit")
                {
                    player.Stop();
                    Print("Goodbye!");
                    break;
                }
                else
                { 
                    Print(string.Format("Cannot recognize {0}, type \"help\" to get more help.", args[0])); 
                }
            }
            player.Save();
        }
    }
}
