using System;
using System.Collections.Generic;
using System.Text;

namespace BilibiliMusicPlayer
{
    class Utils
    {
        private static Random RNG = new Random();

        public static int NextInt(int max) => RNG.Next(max);
        public static int NextInt(int min, int max) => RNG.Next(min, max);
        public static int NextInt() => RNG.Next();
    }
}
