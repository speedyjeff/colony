using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colony
{
    enum BlockType
    {
        None = 0,
        Air = 1,
        Dirt = 2,
        Egg = 3,
        Food = 4,
        DeadAnt = 5,
        WasteDirt = 6
    }

    static class BlockConstants
    {
        public const int FoodFull = 4;
        public const int QueenFull = 4;
        public const int QueenDigest = 200; // 200
        public const int EggHatch = 400; // 400
        public const int AntAdultAge = 4000; // 4000
        public const int AntMaxAge = 4001; // 8000
    }
}
