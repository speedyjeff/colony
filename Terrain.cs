using engine.Common;
using System;

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
        public Terrain(float width, float height, BlockDetails[][] blocks)
        {
            // validate
            if (blocks == null || blocks.Length == 0 || blocks[0].Length == 0) throw new Exception("invalid blocks");

            // init
            Width = width;
            Height = height;
            Columns = blocks[0].Length;
            Rows = blocks.Length;
            BlockWidth = Width / Columns;
            BlockHeight = Height / Rows;
            Previous = new Coordinate() { Row = 0, Column = 0 };
            Blocks = blocks; // todo - copy?

            // initialize for the shortest path
            Path = new ShortestPath(Rows, Columns);

            // initialize the path
            Path.NoUpdates = true;
            {
                for (int r = 0; r < Rows; r++)
                {
                    for (int c = 0; c < Columns; c++)
                    {
                        // mark traversable
                        Path.SetTraversable(r, c, traversable: Blocks[r][c].Type != BlockType.Dirt);

                        // apply the pheromones
                        for(int p=1; p< Blocks[r][c].Pheromones.Length; p++)
                        {
                            if (Blocks[r][c].Pheromones[p] != DirectionType.None)
                            {
                                // update block details
                                SetBlockPheromone(r, c, (PheromoneType)p, Blocks[r][c].Pheromones[p]);
                            }
                        }
                    }
                }
            }
            Path.NoUpdates = false;

            // update the shortest path (all at once)
            Path.Update();
        }

        public float Width { get; private set; }
        public float Height { get; private set; }
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public float BlockWidth { get; private set; }
        public float BlockHeight { get; private set; }
        public float Speed { get; set; }

        public Action<float /* x */, float /* y */> OnAddEgg { get; set; }

        public bool TryApplyPheromone(float x, float y, PheromoneType pheromone)
        {
            if (!TryCoordinatesToRowColumn(x, y, out int r, out int c)) return false;

            // exit early if this block matches the last
            if (Previous.Row == r && Previous.Column == c && PerviousPheromone == pheromone) return false;

            // apply the intent
            Path.NoUpdates = true;
            {
                // mark an initial direction
                SetBlockPheromone(r, c, pheromone, DirectionType.Up);

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
            PerviousPheromone = pheromone;

            return true;
        }

        public bool TryClearPheromone(float x, float y, PheromoneType pheromone)
        {
            if (!TryCoordinatesToRowColumn(x, y, out int r, out int c)) return false;

            // clear the pheromone
            SetBlockPheromone(r, c, pheromone, DirectionType.None);

            return true;
        }

        public bool TryChangeBlockDetails(float x, float y, Movement move, PheromoneType pheromone)
        {
            // adjust x,y based on movement (and Speed)
            x += (move.dX * Speed);
            y += (move.dY * Speed);

            // convert the x,y into column and row
            if (!TryCoordinatesToRowColumn(x, y, out int r, out int c)) return false;

            return TryChangeBlockDetails(r, c, pheromone);
        }

        public bool TryChangeBlockDetails(int row, int col, PheromoneType pheromone)
        {
            // validate the input
            if (col < 0 || col >= Columns || row < 0 || row >= Rows) return false;

            // todo - holding the lock on ShortestPath update?

            // ensure atomic operation on update
            lock (this)
            { 
                // determine how to set based on the current block type
                if (Blocks[row][col].Type == BlockType.Air)
                {
                    // check that we have a drop pheromone
                    if (pheromone == PheromoneType.DropDirt &&
                        Blocks[row][col].Pheromones[(int)PheromoneType.DropDirt] != DirectionType.None)
                    {
                        // set block details
                        Blocks[row][col].Type = BlockType.WasteDirt;

                        // remove the drop pheromone
                        SetBlockPheromone(row, col, PheromoneType.DropDirt, DirectionType.None);

                        // retain as traversable (todo?)
                        return true;
                    }
                    else if (pheromone == PheromoneType.DropFood &&
                        Blocks[row][col].Pheromones[(int)PheromoneType.DropFood] != DirectionType.None)
                    {
                        // set block details
                        Blocks[row][col].Type = BlockType.Food;
                        Blocks[row][col].Counter = 1;

                        return true;
                    }
                    else if (pheromone == PheromoneType.DropEgg &&
                        Blocks[row][col].Pheromones[(int)PheromoneType.DropEgg] != DirectionType.None)
                    {
                        // keep the block details as air

                        // request that an Ant as an Egg be added
                        if (OnAddEgg != null)
                        {
                            // put the egg into the middle of the block
                            OnAddEgg((col * BlockWidth) - (Width / 2) + (BlockWidth / 4), (row * BlockHeight) - (Height / 2) + (BlockHeight / 4));
                        }

                        // remove the drop pheromone
                        SetBlockPheromone(row, col, PheromoneType.DropEgg, DirectionType.None);
                        return true;
                    }
                    else if (pheromone == PheromoneType.MoveQueen)
                    {
                        // the Queen is laying an egg
                        // set block details
                        Blocks[row][col].Type = BlockType.Egg;

                        // add the pheromone
                        SetBlockPheromone(row, col, PheromoneType.MoveEgg, DirectionType.Up);
                        return true;
                    }
                    else if (pheromone == PheromoneType.DropDeadAnt &&
                        Blocks[row][col].Pheromones[(int)PheromoneType.DropDeadAnt] != DirectionType.None)
                    {
                        // set block details
                        Blocks[row][col].Type = BlockType.DeadAnt;

                        // remove the drop pheromone
                        SetBlockPheromone(row, col, PheromoneType.DropDeadAnt, DirectionType.None);

                        return true;
                    }
                }
                else if (Blocks[row][col].Type == BlockType.Dirt)
                {
                    // check that we can pick up this block
                    if (pheromone == PheromoneType.MoveDirt &&
                        Blocks[row][col].Pheromones[(int)PheromoneType.MoveDirt] != DirectionType.None)
                    {
                        // set block details
                        Blocks[row][col].Type = BlockType.Air;

                        // remove the pheromone
                        Path.NoUpdates = true;
                        {
                            SetBlockPheromone(row, col, PheromoneType.MoveDirt, DirectionType.None);
                        }
                        Path.NoUpdates = false;

                        // mark the block as traversable
                        Path.SetTraversable(row, col, traversable: true);
                        return true;
                    }
                }
                else if (Blocks[row][col].Type == BlockType.DeadAnt)
                {
                    // check that we can pick up this block
                    if (pheromone == PheromoneType.MoveDeadAnt &&
                        Blocks[row][col].Pheromones[(int)PheromoneType.MoveDeadAnt] != DirectionType.None)
                    {
                        // set block details
                        Blocks[row][col].Type = BlockType.Air;

                        // remove the pheromone
                        SetBlockPheromone(row, col, PheromoneType.MoveDeadAnt, DirectionType.None);

                        return true;
                    }
                }
                else if (Blocks[row][col].Type == BlockType.Food)
                {
                    // check that we can pick up this block
                    if ((pheromone == PheromoneType.MoveFood &&
                        Blocks[row][col].Pheromones[(int)PheromoneType.MoveFood] != DirectionType.None) ||
                        (pheromone == PheromoneType.MoveQueen))
                    {
                        // reduce the counter
                        Blocks[row][col].Counter--;

                        // remove once gone
                        if (Blocks[row][col].Counter <= 0)
                        {
                            // set block details
                            Blocks[row][col].Type = BlockType.Air;

                            // remove the pheromone
                            SetBlockPheromone(row, col, PheromoneType.MoveFood, DirectionType.None);
                        }

                        return true;
                    }

                    // check if we are dropping off food
                    if (pheromone == PheromoneType.DropFood &&
                        Blocks[row][col].Pheromones[(int)PheromoneType.DropFood] != DirectionType.None &&
                        Blocks[row][col].Counter < BlockConstants.FoodFull)
                    {
                        // increase the counter
                        Blocks[row][col].Counter++;

                        return true;
                    }
                }
                else if (Blocks[row][col].Type == BlockType.Egg)
                {
                    // check that we can pick up this block
                    if (pheromone == PheromoneType.MoveEgg &&
                        Blocks[row][col].Pheromones[(int)PheromoneType.MoveEgg] != DirectionType.None)
                    {
                        // set block details
                        Blocks[row][col].Type = BlockType.Air;

                        // remove the pheromone
                        SetBlockPheromone(row, col, PheromoneType.MoveEgg, DirectionType.None);

                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetBlockDetails(float x, float y, Movement move, out BlockType type, out int count, out DirectionType[] pheromones)
        {
            // adjust x,y based on movement (and Speed)
            x += (move.dX * Speed);
            y += (move.dY * Speed);

            // convert the x,y into column and row
            if (!TryCoordinatesToRowColumn(x, y, out int r, out int c))
            {
                type = BlockType.None;
                pheromones = null;
                count = 0;
                return false;
            }

            return TryGetBlockDetails(r, c, out type, out count, out pheromones);
        }

        public bool TryGetBlockDetails(int r, int c, out BlockType type, out int count, out DirectionType[] pheromones)
        {
            // validate the input
            if (c < 0 || c >= Columns || r < 0 || r >= Rows)
            {
                type = BlockType.None;
                pheromones = null;
                count = 0;
                return false;
            }

            // get the details
            type = Blocks[r][c].Type;
            pheromones = Blocks[r][c].Pheromones;  // todo, do not share a reference
            count = Blocks[r][c].Counter;
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

        public bool TryMove(float x, float y, float width, float height, Movement move)
        {
            // adjust x,y based on movement (and Speed)
            x += (move.dX * Speed);
            y += (move.dY * Speed);

            // check all the points are not within a blocking area
            foreach (var pnt in new engine.Common.Point[]
                {
                    new Point() { X = 0, Y = 0 }, // center
                    new Point() { X = 0 - (width / 2), Y = 0 - (height / 2) }, // top left
                    new Point() { X = 0 + (width / 2), Y = 0 - (height / 2) }, // top right
                    new Point() { X = 0 - (width / 2), Y = 0 + (height / 2) }, // bottom left
                    new Point() { X = 0 + (width / 2), Y = 0 + (height / 2) }, // bottom right
                })
            {
                // get the block type
                if (TryGetBlockDetails(x + pnt.X, y + pnt.Y, move, out BlockType block, out int count, out DirectionType[] pheromones))
                {
                    // check the block type
                    if (IsBlocking(block)) return false;
                }
                else
                {
                    // out of bounds
                    return false;
                }
            }

            return true;
        }

        public bool TryCoordinatesToRowColumn(float x, float y, out int r, out int c)
        {   
            // todo - assumes X,Y of (0,0)

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

        #region private
        struct Coordinate
        {
            public int Row;
            public int Column;
        }
        private BlockDetails[][] Blocks;
        private Coordinate Previous;
        private PheromoneType PerviousPheromone;
        private ShortestPath Path;

        private void SetBlockPheromone(int r, int c, PheromoneType pheromone, DirectionType direction)
        {
            // set the block details
            Blocks[r][c].Pheromones[(int)pheromone] = direction;

            // record in the ShortestPath
            Path.SetPheromone(r, c, pheromone, direction);
        }

        private bool IsBlocking(BlockType block)
        {
            return (block == BlockType.Dirt);
        }
        #endregion
    }
}
