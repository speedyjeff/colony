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

            // initialize for the shortest path
            Path = new ShortestPath(Rows, Columns);

            // initialize the blocks
            Blocks = new BlockDetails[Rows][];
            Path.NoUpdates = true;
            {
                for (int r = 0; r < Rows; r++)
                {
                    Blocks[r] = new BlockDetails[Columns];
                    for (int c = 0; c < Columns; c++)
                    {
                        // add dirt and air
                        if (r >= Rows / 2)
                        {
                            // update block details
                            Blocks[r][c].Type = BlockType.Dirt;

                            // mark on the ShortestPath
                            Path.SetTraversable(r, c, traversable: false);
                        }
                        else
                        {
                            // update block details
                            Blocks[r][c].Type = BlockType.Air;
                        }
                        // set to default - no pheromones
                        Blocks[r][c].Pheromones = new DirectionType[]
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
            }
            Path.NoUpdates = false;
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
            Path.NoUpdates = true;
            {
                switch (pheromone)
                {
                    case PheromoneType.DropDirt:
                    case PheromoneType.MoveDirt:
                    case PheromoneType.MoveQueen:
                        // mark an initial direction
                        SetBlockPheromone(r, c, pheromone, DirectionType.Up);
                        break;
                    default:
                        throw new Exception("invalid action");
                }

                // set direction
                if (Previous.Row >= 0 && Previous.Column >= 0 && Previous.Row < Rows && Previous.Column < Columns)
                {
                    // check up
                    if (Previous.Row == r - 1 && Previous.Column == c &&
                        (Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone]) != DirectionType.None)
                    {
                        // the pheromones are applied moving top down, inherit
                        // set both to down
                        SetBlockPheromone(Previous.Row, Previous.Column, pheromone, DirectionType.Down);
                        SetBlockPheromone(r, c, pheromone, DirectionType.Down);
                    }
                    // check right
                    else if (Previous.Row == r && Previous.Column == c + 1 &&
                        (Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone]) != DirectionType.None)
                    {
                        // the pheromones are applied moving right to left, inherit
                        // set both to left
                        SetBlockPheromone(Previous.Row, Previous.Column, pheromone, DirectionType.Left);
                        SetBlockPheromone(r, c, pheromone, DirectionType.Left);
                    }
                    // check down
                    else if (Previous.Row == r + 1 && Previous.Column == c &&
                        (Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone]) != DirectionType.None)
                    {
                        // the pheromones are applied moving bottom up, inherit
                        // set both to up
                        SetBlockPheromone(Previous.Row, Previous.Column, pheromone, DirectionType.Up);
                        SetBlockPheromone(r, c, pheromone, DirectionType.Up);
                    }
                    // check left
                    else if (Previous.Row == r && Previous.Column == c - 1 &&
                        (Blocks[Previous.Row][Previous.Column].Pheromones[(int)pheromone]) != DirectionType.None)
                    {
                        // the pheromones are applied moving left to right, inherit
                        // set both to right
                        SetBlockPheromone(Previous.Row, Previous.Column, pheromone, DirectionType.Right);
                        SetBlockPheromone(r, c, pheromone, DirectionType.Right);
                    }
                }
            }
            Path.NoUpdates = false;
            Path.Update(pheromone);

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
                case PheromoneType.MoveQueen:
                    // mark an initial direction
                    SetBlockPheromone(r, c, pheromone, DirectionType.None);
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
                        // set block details
                        Blocks[r][c].Type = BlockType.Air;

                        // remove the pheromone
                        Path.NoUpdates = true;
                        {
                            SetBlockPheromone(r, c, PheromoneType.MoveDirt, DirectionType.None);
                        }
                        Path.NoUpdates = false;

                        // mark the block as traversable
                        Path.SetTraversable(r, c, traversable: true);
                        return true;
                    }
                }
                else if (block == BlockType.Dirt)
                {
                    // if this is an Air block, change it to WasteDirt
                    if (Blocks[r][c].Type == BlockType.Air)
                    {
                        // set block details
                        Blocks[r][c].Type = BlockType.WasteDirt;

                        // remove the drop pheromone
                        SetBlockPheromone(r, c, PheromoneType.DropDirt, DirectionType.None);

                        // retain as traversable (todo?)
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }

        public bool TryGetBlockDetails(float x, float y, Movement move, out BlockType type, out DirectionType[] pheromones)
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

        public bool TryGetBlockDetails(int r, int c, out BlockType type, out DirectionType[] pheromones)
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

        public bool TryGetBestMove(float x, float y, PheromoneType following, out bool[] directions)
        {
            // convert the x,y into column and row
            if (!TryCoordinatesToRowColumn(x, y, out int r, out int c))
            {
                directions = null;
                return false;
            }

            // find the shortest path
            directions = Path.GetNextMove(r, c, following);
            return true;
        }

        public bool TryCoordinatesToRowColumn(float x, float y, out int r, out int c)
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
            public DirectionType[] Pheromones;
        }
        struct Coordinate
        {
            public int Row;
            public int Column;
        }
        private BlockDetails[][] Blocks;
        private Coordinate Previous;
        private ShortestPath Path;

        private void SetBlockPheromone(int r, int c, PheromoneType pheromone, DirectionType direction)
        {
            // set the block details
            Blocks[r][c].Pheromones[(int)pheromone] = direction;

            // record in the ShortestPath
            Path.SetPheromone(r, c, pheromone, direction);
        }

        #endregion
    }
}
