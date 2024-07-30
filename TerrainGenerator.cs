using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colony
{
    static class TerrainGenerator
    {
        public static BlockDetails[][] SplitInHalf(int rows, int columns)
        {
            var blocks = new BlockDetails[rows][];
            for (int r = 0; r < blocks.Length; r++)
            {
                blocks[r] = new BlockDetails[columns];
                for (int c = 0; c < blocks[r].Length; c++)
                {
                    // add dirt and air
                    if (r >= rows / 2) blocks[r][c].Type = BlockType.Dirt;
                    else blocks[r][c].Type = BlockType.Air;
   
                    // set to default - no pheromones
                    blocks[r][c].Pheromones = new DirectionType[]
                        {
                            DirectionType.None, // None
                            DirectionType.None, // MoveDirt
                            DirectionType.None, // MoveEgg
                            DirectionType.None, // MoveFood
                            DirectionType.None, // MoveDeadAnt
                            DirectionType.None, // MoveQueen
                            DirectionType.None, // DropDirt
                            DirectionType.None, // DropEgg
                            DirectionType.None, // DropFood
                            DirectionType.None, // DropDeadAnt
                        };
                }
            }

            // add some food
            blocks[(rows / 2) - 1][columns - 2].Type = BlockType.Food;
            blocks[(rows / 2) - 1][columns - 2].Counter = BlockConstants.FoodFull;
            blocks[(rows / 2) - 2][columns - 2].Type = BlockType.Food;
            blocks[(rows / 2) - 2][columns - 2].Counter = BlockConstants.FoodFull;

            // debug egg
            blocks[(rows / 2) - 1][0].Type = BlockType.Egg;

            return blocks;
        }

        public static BlockDetails[][] Demo()
        {
            // return a demo terrain that highlights the capabilities of the system
            return null;
        }

        public static BlockDetails[][] DemoRound()
        {
            // todo
            return null;
        }
    }
}
