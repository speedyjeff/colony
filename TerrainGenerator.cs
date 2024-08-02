using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colony
{
    struct PlayerDetails
    {
        public float X;
        public float Y;
        public PheromoneType Pheromone;
    }

    static class TerrainGenerator
    {
        public static void SplitInHalf(int rows, int columns, out BlockDetails[][] blocks, out PlayerDetails[] players)
        {
            blocks = new BlockDetails[rows][];
            for (int r = 0; r < blocks.Length; r++)
            {
                blocks[r] = new BlockDetails[columns];
                for (int c = 0; c < blocks[r].Length; c++)
                {
                    blocks[r][c] = new BlockDetails();
                    // add dirt and air
                    if (r >= rows / 2) blocks[r][c].Type = BlockType.Dirt;
                    else blocks[r][c].Type = BlockType.Air;
   
                    // set to default - no pheromones
                }
            }

            // add some food
            blocks[(rows / 2) - 1][columns - 2].Type = BlockType.Food;
            blocks[(rows / 2) - 1][columns - 2].Counter = BlockConstants.FoodFull;
            blocks[(rows / 2) - 2][columns - 2].Type = BlockType.Food;
            blocks[(rows / 2) - 2][columns - 2].Counter = BlockConstants.FoodFull;

            // debug egg
            blocks[(rows / 2) - 1][0].Type = BlockType.Egg;

            // debug dead ant
            blocks[(rows / 2) - 2][1].Type = BlockType.DeadAnt;

            // add default players
            players = new PlayerDetails[]
            {
                new PlayerDetails() { X = -10, Y = -100, Pheromone = PheromoneType.MoveDirt },
                new PlayerDetails() { X = -20, Y = -100, Pheromone = PheromoneType.MoveDirt },
                new PlayerDetails() { X = -30, Y = -100, Pheromone = PheromoneType.MoveDirt },
                new PlayerDetails() { X = 0, Y = -100, Pheromone = PheromoneType.MoveQueen },
                new PlayerDetails() { X = 0, Y = -100, Pheromone = PheromoneType.MoveFood },
                new PlayerDetails() { X = 0, Y = -100, Pheromone = PheromoneType.MoveEgg },
                new PlayerDetails() { X = 0, Y = -100, Pheromone = PheromoneType.MoveDeadAnt },
            };
        }

        public static void BigEmpty(out BlockDetails[][] blocks, out PlayerDetails[] players)
        {
            // empty
            var rows = 100;
            var cols = 100;
            blocks = new BlockDetails[rows][];
            for (int r = 0; r < blocks.Length; r++)
            {
                blocks[r] = new BlockDetails[cols];
                for (int c = 0; c < blocks[r].Length; c++)
                {
                    blocks[r][c] = new BlockDetails();
                    blocks[r][c].Type = BlockType.Air;
                }
            }

            // add a lot of different Ants
            var numAnts = 1000;
            players = new PlayerDetails[numAnts];
            for (int i = 0; i < numAnts; i++)
            {
                players[i] = new PlayerDetails()
                {
                    X = 0,
                    Y = 0,
                    Pheromone = (PheromoneType)(i % 5 + 1)
                };
            }
        }

            public static BlockDetails[][] Demo()
        {
            // return a demo terrain that highlights the capabilities of the system
            return null;
        }

        public static BlockDetails[][] DemoRound()
        {
            var columns = 100;
            var rows = 100;
            int centerC = columns / 2;
            int centerR = rows / 2;
            int radius = Math.Min(centerC, centerR) - (columns/10);

            // put the dirt in a circle with a hole in the middle
            var blocks = new BlockDetails[rows][];
            for (int r = 0; r < blocks.Length; r++)
            {
                blocks[r] = new BlockDetails[columns];
                for (int c = 0; c < blocks[r].Length; c++)
                {
                    var dc = c - centerC;
                    var dr = r - centerR;
                    blocks[r][c] = new BlockDetails();
                    if (dc * dc + dr * dr <= radius * radius) blocks[r][c].Type = BlockType.Dirt;
                    else blocks[r][c].Type = BlockType.Air;
                }
            }

            return blocks;
        }
    }
}
