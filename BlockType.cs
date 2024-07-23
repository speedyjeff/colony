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
        WasteDirt = 6,
        WasteDeadAnt = 7
    }

    static class BlockConstants
    {
        public const int FoodFull = 4;
        public const int EggFull = 2;
        public const int QueenFull = 4;
    }
}
