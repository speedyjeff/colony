﻿using System;

namespace colony
{
    struct BlockDetails
    {
        public BlockType Type;
        public int Counter;
        public DirectionType[] Pheromones;

        public BlockDetails()
        {
            Type = BlockType.None;
            Counter = 0;
            Pheromones = new DirectionType[]
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

        public static BlockDetails[][] Create(int rows, int columns)
        {
            var blocks = new BlockDetails[rows][];
            for (var r = 0; r < rows; r++)
            {
                blocks[r] = new BlockDetails[columns];
                for (var c = 0; c < columns; c++)
                {
                    blocks[r][c] = new BlockDetails();
                }
            }
            return blocks;
        }
    }
}
