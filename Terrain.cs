using engine.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colony
{
    // Type State machine
    //  Air - can become any type
    //  Dirt/Food/DeadAnt - can become Air, are moveable, are blocking
    //  Egg - will become Air AND a new Ant will appear, is moveable, is blocking
    //  Waste* - is not moveable, is not blocking
    
    // Pheromones
    //  None - will remove all pheromones
    //  MoveDirt - can be placed on any block, will cause dirt to be moved
    //  MoveEgg - can only be placed on Air, will cause an egg to be moved
    //  MoveFood - can only be placed on Air, will cause food to be moved
    //  MoveDeadAnt - can only be placed on Air, will cause a dead ant to be moved
    //  MoveQueen - can only be placed on Air, will cause a Queen to move

    // Pheromone Direction
    //  Pheromones directions are inherited from their neighbors, first wins
    //  The previous cell updated is tracked and if a neighbor of the current cell, they both change
    //    the current and previous cell are updated together
    //     1
    //  4 [ ] 2
    //     3
    //
    //  A few examples
    //   N | D | N
    //   U | ? | R  ? == D 
    //   N | L | N
    //
    //                        Over time
    //   * | * | * | * | *     prev cell = x: 1 = U, 2,3,4,5
    //   * | * | * | * | *     prev cell = 1: 1 = R, 2 = R, 3,4,5
    //   * | * | 5 | * | *     prev cell = 2: 1 = R, 2 = R, 3 = R, 4,5
    //   * | * | 4 | * | *     prev cell = 3: 1 = R, 2 = R, 3 = U, 4 = U, 5
    //   1 | 2 | 3 | * | *     prev cell = 4: 1 = R, 2 = R, 3 = U, 4 = U, 5 = U

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

    enum PheromoneDirectionType
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4
    }

    class Terrain
    {
        public Terrain(float width, float height, int columns, int rows)
        {
            // init
            Width = width;
            Height = height;
            Columns = columns;
            Rows = rows;
            BlockWidth = Width / Columns;
            BlockHeight = Height / Rows;
            Previous = new Coordinate() { Row = 0, Column = 0 };

            // initialize the blocks
            Blocks = new BlockDetails[Rows][];
            for (int r = 0; r < Rows; r++)
            {
                Blocks[r] = new BlockDetails[Columns];
                for (int c = 0; c < Columns; c++)
                {
                    // add dirt and air
                    if (r >= Rows / 2) Blocks[r][c].Type = BlockType.Dirt;
                    else Blocks[r][c].Type = BlockType.Air;
                    // set to default - no pheromones
                    Blocks[r][c].Pheromones = new PheromoneDirectionType[]
                        {
                            PheromoneDirectionType.None, // None
                            PheromoneDirectionType.None, // MoveDirt
                            PheromoneDirectionType.None, // MoveEgg
                            PheromoneDirectionType.None, // MoveFood
                            PheromoneDirectionType.None, // MoveDeadAnt
                            PheromoneDirectionType.None, // MoveQueen
                            PheromoneDirectionType.None, // DropDirt
                            PheromoneDirectionType.None, // DropEgg
                            PheromoneDirectionType.None, // DropFood
                            PheromoneDirectionType.None, // DropDeadAnt
                        };
                }
            }
        }

        public float Width { get; private set; }
        public float Height { get; private set; }
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public float BlockWidth { get; private set; }
        public float BlockHeight { get; private set; }
        public float Speed { get; set; }

        public void ApplyPheromone(float x, float y, PheromoneType pheromone)
        {
            // NOTE - x,y are block relative (eg. 0,0, width, height)
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new Exception("invalid x,y");

            // translate the x,y into a row and column
            var c = (int)(x / BlockWidth);
            var r = (int)(y / BlockHeight);

            if (c < 0 || c >= Columns || r < 0 || r >= Rows) throw new Exception("invalid index calculated");

            // exit early if this block matches the last
            if (Previous.Row == r && Previous.Column == c) return;

            // apply the intent
            switch (pheromone)
            {
                case PheromoneType.DropDirt:
                case PheromoneType.MoveDirt:
                    // mark an initial direction
                    Blocks[r][c].Pheromones[(int)pheromone] = PheromoneDirectionType.Up;
                    break;
                case PheromoneType.None:
                    // clear
                    for (int i=0; i < Blocks[r][c].Pheromones.Length; i++)
                    {
                        Blocks[r][c].Pheromones[i] = PheromoneDirectionType.None;
                    }
                    break;
                default:
                    throw new Exception("invalid action");
            }

            // set direction
            if (Previous.Row >= 0 && Previous.Column >= 0 && Previous.Row < Rows && Previous.Column < Columns)
            {
                // check up
                if (Previous.Row == r - 1 && Previous.Column == c &&
                    (Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone]) != PheromoneDirectionType.None)
                {
                    // the pheromones are applied moving top down, inherit
                    // set both to down
                    Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone] = PheromoneDirectionType.Down;
                    Blocks[r][c].Pheromones[(int)pheromone] = PheromoneDirectionType.Down;
                }
                // check right
                else if (Previous.Row == r && Previous.Column == c + 1 &&
                    (Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone]) != PheromoneDirectionType.None)
                {
                    // the pheromones are applied moving right to left, inherit
                    // set both to left
                    Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone] = PheromoneDirectionType.Left;
                    Blocks[r][c].Pheromones[(int)pheromone] = PheromoneDirectionType.Left;
                }
                // check down
                else if (Previous.Row == r + 1 && Previous.Column == c &&
                    (Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone]) != PheromoneDirectionType.None)
                {
                    // the pheromones are applied moving bottom up, inherit
                    // set both to up
                    Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone] = PheromoneDirectionType.Up;
                    Blocks[r][c].Pheromones[(int)pheromone] = PheromoneDirectionType.Up;
                }
                // check left
                else if (Previous.Row == r && Previous.Column == c - 1 &&
                    (Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone]) != PheromoneDirectionType.None)
                {
                    // the pheromones are applied moving left to right, inherit
                    // set both to right
                    Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone] = PheromoneDirectionType.Right;
                    Blocks[r][c].Pheromones[(int)pheromone] = PheromoneDirectionType.Right;
                }
            }

            // set previous
            Previous.Row = r;
            Previous.Column = c;
        }

        public void ClearPheromone(float x, float y, PheromoneType pheromone)
        {
            // NOTE - x,y are block relative (eg. 0,0, width, height)
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new Exception("invalid x,y");

            // translate the x,y into a row and column
            var c = (int)(x / BlockWidth);
            var r = (int)(y / BlockHeight);

            if (c < 0 || c >= Columns || r < 0 || r >= Rows) throw new Exception("invalid index calculated");

            // clear the pheromone
            switch (pheromone)
            {
                case PheromoneType.DropDirt:
                case PheromoneType.MoveDirt:
                    // mark an initial direction
                    Blocks[r][c].Pheromones[(int)pheromone] = PheromoneDirectionType.None;
                    break;
                default:
                    throw new Exception("invalid action");
            }
        }

        public bool TrySetBlockDetails(float x, float y, Movement move, BlockType block)
        {
            // adjust x,y based on movement (and Speed)
            x += (move.dX * Speed);
            y += (move.dY * Speed);

            // todo - assumes X,Y of (0,0)
            // convert the x,y into column and row
            if (!TryCoordinatesToRowColumn(x, y, out int r, out int c)) return false;

            lock (this)
            {
                // determine what action to take
                if (block == BlockType.Air)
                {
                    // if this is a Dirt block, change it to Air
                    if (Blocks[r][c].Type == BlockType.Dirt)
                    {
                        Blocks[r][c].Type = BlockType.Air;
                        return true;
                    }
                }
                else if (block == BlockType.Dirt)
                {
                    // if this is an Air block, change it to WasteDirt
                    if (Blocks[r][c].Type == BlockType.Air)
                    {
                        Blocks[r][c].Type = BlockType.WasteDirt;
                        return true;
                    }
                    return false;
                }
                /*
                else if (block == BlockType.Egg)
                {
                    // if this is an Air block, change it to Egg
                    if (Blocks[(int)r][(int)c].Type == BlockType.Air)
                    {
                        Blocks[(int)r][(int)c].Type = BlockType.Egg;
                        return true;
                    }
                }
                else if (block == BlockType.Food)
                {
                    // if this is an Air block, change it to Food
                    if (Blocks[(int)r][(int)c].Type == BlockType.Air)
                    {
                        Blocks[(int)r][(int)c].Type = BlockType.Food;
                        return true;
                    }
                }
                else if (block == BlockType.DeadAnt)
                {
                    // if this is an Air block, change it to DeadAnt
                    if (Blocks[(int)r][(int)c].Type == BlockType.Air)
                    {
                        Blocks[(int)r][(int)c].Type = BlockType.DeadAnt;
                        return true;
                    }
                }
                */
            }

            return false;
        }

        public bool TryGetBlockDetails(float x, float y, Movement move, out BlockType type, out PheromoneDirectionType[] pheromones)
        {
            // adjust x,y based on movement (and Speed)
            x += (move.dX * Speed);
            y += (move.dY * Speed);

            // convert the x,y into column and row
            if (!TryCoordinatesToRowColumn(x, y, out int r, out int c))
            {
                type = BlockType.None;
                pheromones = null;
                return false;
            }

            return TryGetBlockDetails(r, c, out type, out pheromones);
        }

        public bool TryGetBlockDetails(int r, int c, out BlockType type, out PheromoneDirectionType[] pheromones)
        {
            // validate the input
            if (c < 0 || c >= Columns || r < 0 || r >= Rows)
            {
                type = BlockType.None;
                pheromones = null;
                return false;
            }

            // get the details
            type = Blocks[r][c].Type;
            pheromones = Blocks[r][c].Pheromones;  // todo, do not share a reference
            return true;
        }

        public bool IsBlocking(BlockType block)
        {
            return (block == BlockType.Dirt);
        }

        public bool IsMoveable(BlockType block)
        {
            return (block == BlockType.Egg) ||
                (block == BlockType.Dirt) ||
                (block == BlockType.Food);
        }

        #region private
        struct BlockDetails
        {
            public BlockType Type;
            public PheromoneDirectionType[] Pheromones;
        }
        struct Coordinate
        {
            public int Row;
            public int Column;
        }
        private BlockDetails[][] Blocks;
        private Coordinate Previous;

        private bool TryCoordinatesToRowColumn(float x, float y, out int r, out int c)
        {
            // convert the x,y into column and row
            var fr = ((y + (Height / 2)) / BlockHeight);
            var fc = ((x + (Width / 2)) / BlockWidth);

            // check the boundaries (int divide does not take negative zero into account)
            if (fr < 0 || fc < 0)
            {
                r = -1;
                c = -1;
                return false;
            }

            r = (int)Math.Floor(fr);
            c = (int)Math.Floor(fc);
            return true;
        }
        #endregion
    }
}
