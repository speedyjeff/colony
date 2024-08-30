using engine.Common.Entities;
using engine.Common.Entities3D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        public static void Demo(int rows, int columns, out BlockDetails[][] blocks, out PlayerDetails[] players)
        {
            var random = new Random();

            // initialize the blocks
            blocks = new BlockDetails[rows][];
            for (int r = 0; r < blocks.Length; r++)
            {
                blocks[r] = new BlockDetails[columns];
                for (int c = 0; c < blocks[r].Length; c++)
                {
                    blocks[r][c] = new BlockDetails();
                    // add dirt and air
                    if (r >= rows / 3) blocks[r][c].Type = BlockType.Dirt;
                    else blocks[r][c].Type = BlockType.Air;

                    // set to default - no pheromones
                }
            }

            // pick three tunnel entrances
            var blockHeight = 100;
            var blockWidth = 100;
            var entranceRow = (rows / 3);
            var entranceY = -1 * (blockHeight) * ((rows/2) - entranceRow);
            var entranceColumns = new int[]
            {
                (columns / 4), 
                (columns / 2),
                ((2 * columns) / 3)
            };

            // add dirt piles for extracted dirt
            foreach(var col in entranceColumns)
            {
                DirtPile(blocks, entranceRow, col);
            }

            // remove the drop dirt from the entrances
            foreach (var col in entranceColumns)
            {
                RemoveDirtPile(blocks, entranceRow, col);
            }

            // add tunnels for the ants to dig
            foreach (var col in entranceColumns)
            {
                CreateTunnels(blocks, entranceRow, col);
            }

            // add interesting rooms off of the tunnels (out of pheromones)
            foreach(var col in entranceColumns)
            {
                CreateRooms(random, 
                    blocks, 
                    entranceRow, 
                    col, 
                    rooms: new PheromoneType[] 
                    {
                        PheromoneType.MoveQueen,
                        PheromoneType.MoveFood,
                        PheromoneType.DropEgg,
                        PheromoneType.DropDeadAnt,
                        PheromoneType.DropDeadAnt,
                        PheromoneType.MoveFood,
                        PheromoneType.DropEgg,
                        PheromoneType.MoveFood,
                        PheromoneType.MoveFood,
                        PheromoneType.DropDeadAnt
                    });
            }

            // add a horizontal tunnel that connects all the vertical tunnels
            var verticalTunnelColumns = new int[]
            {
                5,
                columns - 15
            };
            var verticalColumnWidth = 10;
            var verticalRowStart = entranceRow + 5;

            // add a horizontal tunnel across the bottom
            for (int c = verticalTunnelColumns[0] + (verticalColumnWidth/2); c < verticalTunnelColumns[1] + (verticalColumnWidth/2); c++)
            {
                var dir = c < entranceColumns[1] ? DirectionType.Left : DirectionType.Right;
                if (blocks[rows - 2][c].Type == BlockType.Dirt) blocks[rows-2][c].Pheromones[(int)PheromoneType.MoveDirt] = dir;
            }

            // dig vertical tunnels on either side to the top
            for (int r = verticalRowStart; r < rows - 2; r++)
            {
                for (int c = 0; c < verticalTunnelColumns.Length; c++)
                {
                    for (int i = 0; i < verticalColumnWidth; i++)
                    {
                        blocks[r][verticalTunnelColumns[c] + i].Pheromones[(int)PheromoneType.MoveDirt] = DirectionType.Up;
                    }
                }
            }

            // add rows of eggs and fruit
            for (int r = verticalRowStart; r < verticalRowStart+5; r++)
            {
                for (int c = 0; c < verticalTunnelColumns.Length; c++)
                {
                    for (int i = 0; i < verticalColumnWidth; i++)
                    {
                        // clear the pheromones
                        blocks[r][verticalTunnelColumns[c] + i].Pheromones[(int)PheromoneType.MoveDirt] = DirectionType.None;

                        // add eggs or food
                        if (random.Next() % 2 == 0)
                        {
                            blocks[r][verticalTunnelColumns[c] + i].Type = BlockType.Egg;
                            blocks[r][verticalTunnelColumns[c] + i].Counter = 1;
                        }
                        else
                        {
                            blocks[r][verticalTunnelColumns[c] + i].Type = BlockType.Food;
                            blocks[r][verticalTunnelColumns[c] + i].Counter = BlockConstants.FoodFull;
                        }
                    }
                }
            }

            // add a string of ants across the top
            var ants = new List<PlayerDetails>();
            for (int c = -1 * (rows / 2) * blockWidth + (blockWidth/2); c < (rows / 2) * blockWidth; c+=blockWidth)
            {
                var pheromone = PheromoneType.MoveDirt;
                switch (random.Next() % 15)
                {
                    case 0:
                        pheromone = PheromoneType.MoveQueen;
                        break;
                    case 1:
                    case 2:
                        pheromone = PheromoneType.MoveFood;
                        break;
                    case 3:
                        pheromone = PheromoneType.MoveEgg;
                        break;
                    case 4:
                    case 5:
                        pheromone = PheromoneType.MoveDeadAnt;
                        break;
                    default:
                        pheromone = PheromoneType.MoveDirt;
                        break;

                }

                ants.Add(new PlayerDetails() { X = c, Y = -1 * (rows / 2) * blockHeight + blockHeight, Pheromone = pheromone});
            }
            players = ants.ToArray();
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

        #region private
        private static void DirtPile(BlockDetails[][] blocks, int row, int col)
        {
            // add dirt piles for extracted dirt
            // build a pyramid of DropDirt pheromones from 0 to row at col
            for(int r = 0; r < row; r++)
            {
                // build a pyramid
                for (int c = col - r; c <= col + r; c++)
                {
                    if (c < 0 || c >= blocks[r].Length) continue;
                    blocks[r][c].Pheromones[(int)PheromoneType.DropDirt] = DirectionType.Up;
                }
            }
        }

        private static void RemoveDirtPile(BlockDetails[][] blocks, int row, int col)
        {
            // remove the drop dirt from the entrances
            for (int r = 0; r < row; r++)
            {
                blocks[r][col].Pheromones[(int)PheromoneType.DropDirt] = DirectionType.None;
            }
        }

        private static void CreateTunnels(BlockDetails[][] blocks, int row, int col)
        {
            // add tunnels for the ants to dig
            // build a tunnel from row to the bottom
            for (int r = row; r < blocks.Length-2; r++)
            {
                blocks[r][col].Pheromones[(int)PheromoneType.MoveDirt] = DirectionType.Down;
            }
        }

        private static void CreateRooms(Random random, BlockDetails[][] blocks, int row, int col, PheromoneType[] rooms)
        {
            // room pattern
            var room = new byte[][]
                {
                    new byte[] {0, 0, 1, 1, 0, 0 },
                    new byte[] {0, 1, 1, 1, 1, 0 },
                    new byte[] {1, 1, 1, 2, 1, 1 },
                    new byte[] {0, 1, 1, 1, 1, 0 },
                    new byte[] {0, 0, 1, 1, 0, 0 },
                };

            // randomly sort the room pheromones
            for(int i=0; i<rooms.Length; i++)
            {
                var index = 0;
                do
                {
                    index = random.Next() % rooms.Length;
                }
                while (i == index);
                // swap
                var temp = rooms[i];
                rooms[i] = rooms[index];
                rooms[index] = temp;
            }

            // add interesting rooms off of the tunnels (out of pheromones)
            var roomRow = row + 1;
            var roomCol = 0;
            var blocksPerRoom = (blocks.Length - row) / rooms.Length;
            for (int i = 0; i < rooms.Length; i++)
            {
                // choose which side
                var onLeft = random.Next() % 2 == 0;

                // pick a location of the room
                //roomRow = random.Next(row + room.Length + 1, blocks.Length - room.Length - 1);
                roomRow += room.Length + 1;
                roomCol = onLeft ? (col - room[0].Length) : (col + 1);
                if (roomCol < 0 || roomCol >= blocks[0].Length) throw new Exception("invalid room row,col");

                // add the room
                var dir = onLeft ? DirectionType.Left : DirectionType.Right;
                for (int r = 0; r < room.Length; r++)
                {
                    for (int c = 0; c < room[0].Length; c++)
                    {
                        // skip
                        if (room[r][c] == 0) continue;

                        // dig out the room pattern
                        if (blocks[r + roomRow][c + roomCol].Type == BlockType.Dirt) blocks[r + roomRow][c + roomCol].Pheromones[(int)PheromoneType.MoveDirt] = dir;

                        // place a pheromone of what type of room this is
                        if (rooms[i] == PheromoneType.MoveQueen)
                        {
                            // only place the queen in the middle
                            if (room[r][c] == 2)
                            {
                                // mark for a queen
                                blocks[r + roomRow][c + roomCol].Pheromones[(int)rooms[i]] = dir;

                                // mark for food drop
                                blocks[r + roomRow][c + roomCol].Pheromones[(int)PheromoneType.DropFood] = dir;
                            }
                        }
                        else
                        {
                            // mark with pheromone
                            blocks[r + roomRow][c + roomCol].Pheromones[(int)rooms[i]] = dir;
                        }

                        // check if this is food, in which case we need to replace the room with food
                        if (rooms[i] == PheromoneType.MoveFood)
                        {
                            // mark as food
                            blocks[r + roomRow][c + roomCol].Type = BlockType.Food;
                            blocks[r + roomRow][c + roomCol].Counter = BlockConstants.FoodFull;

                            // remove the need to dig
                            blocks[r + roomRow][c + roomCol].Pheromones[(int)PheromoneType.MoveDirt] = DirectionType.None;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
