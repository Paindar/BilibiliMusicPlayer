using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BilibiliMusicPlayer
{
    class BVConvert
    {
        private static string _str = "fZodR9XQDSUm21yCkr6zBqiveYah8bt4xsWpHnJE7jL5VG3guMTKNPAwcF";
        private static Dictionary<char, int> _dict = new Dictionary<char, int>()
        {
            { 'f', 0 },
            { 'Z', 1 },
            { 'o', 2 },
            { 'd', 3 },
            { 'R', 4 },
            { '9', 5 },
            { 'X', 6 },
            { 'Q', 7 },
            { 'D', 8 },
            { 'S', 9 },
            { 'U', 10 },
            { 'm', 11 },
            { '2', 12 },
            { '1', 13 },
            { 'y', 14 },
            { 'C', 15 },
            { 'k', 16 },
            { 'r', 17 },
            { '6', 18 },
            { 'z', 19 },
            { 'B', 20 },
            { 'q', 21 },
            { 'i', 22 },
            { 'v', 23 },
            { 'e', 24 },
            { 'Y', 25 },
            { 'a', 26 },
            { 'h', 27 },
            { '8', 28 },
            { 'b', 29 },
            { 't', 30 },
            { '4', 31 },
            { 'x', 32 },
            { 's', 33 },
            { 'W', 34 },
            { 'p', 35 },
            { 'H', 36 },
            { 'n', 37 },
            { 'J', 38 },
            { 'E', 39 },
            { '7', 40 },
            { 'j', 41 },
            { 'L', 42 },
            { '5', 43 },
            { 'V', 44 },
            { 'G', 45 },
            { '3', 46 },
            { 'g', 47 },
            { 'u', 48 },
            { 'M', 49 },
            { 'T', 50 },
            { 'K', 51 },
            { 'N', 52 },
            { 'P', 53 },
            { 'A', 54 },
            { 'w', 55 },
            { 'c', 56 },
            { 'F', 57 }
        };
        private static int[] _s = { 11, 10, 3, 8, 4, 6, 2, 9, 5, 7 };
        private static long xor = 177451812;
        private static long add = 100618342136696320;

        public static string bv2av(string bv)
        {
            if (!bv.StartsWith("BV"))
            {
                bv = "BV" + bv;
            }
            long r = 0;
            for(int i = 0;i<10;i++)
            {
                r += (long)(_dict[bv[_s[i]]] * Math.Pow(58, i));
            }
            return ((r - add) ^ xor).ToString();
        }

        public static string av2bv(string avid)
        {
            if (avid.StartsWith("av"))
                avid = avid.Substring(2);
            long av = int.Parse(avid);
            av = (av ^ xor) + add;
            char[] r = new char[12];
            r[0] = 'B';r[1] = 'V';
            for (int i = 0; i < 10; i++)
            {
                long step1 = (long)Math.Pow(58, i);
                int halfPaht = (int)(av / step1 % 58);
                r[_s[i]] = _str[halfPaht];
            }
            string ret = new string(r);
            return ret;
        }

        public static string video_trbv(string video)
        {
            int up = video.IndexOf("/BV");
            int down = video.IndexOf('?', up + 3);
            if (up != -1)
            {
                if (down != -1)
                    return video.Substring(up + 1, down);
                else
                    return video.Substring(up + 1);
            }
            else if (video.Contains("BV"))
            {
                return video;
            }
            else
            {
                if (video.Length == 10)
                    return "BV" + video;
                else
                    return "";
            }
        }

        public static string video_trav(string video)
        {
            video = video.ToLower();
            int up = video.IndexOf("/av");
            int down = video.IndexOf('?', up + 3);
            if (up != -1)
            {
                if (down != -1)
                    return video.Substring(up + 1, down);
                else
                    return video.Substring(up + 1);
            }
            else if (video.Contains("av"))
            {
                return video;
            }
            else
            {
                int ret;
                if (int.TryParse(video, out ret))
                {
                    return "av" + ret.ToString();
                }
                else
                    return "";
            }
        }

        public static bool isAV(string avid)
        {
            bool ret = true;
            try
            {
                avid = video_trav(avid);
                if (avid == "") return false;
                string bv = av2bv(avid);
                string av = bv2av(bv);
                ret = (avid.Equals(av));
            }
            catch(Exception)
            {
                ret = false;
            }
            return ret;
        }

        public static bool isBV(string bvid)
        {
            bool ret = true;
            try
            {
                bvid = video_trbv(bvid);
                string av = bv2av(bvid);
                string bv = av2bv(av);
                ret = (bvid.Equals(bv));
            }
            catch (Exception)
            {
                ret = false;
            }
            return ret;
        }
    }
}
