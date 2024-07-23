using System;
using System.Collections.Generic;
using engine.Common;
using engine.Common.Entities;
using engine.Common.Entities.AI;

namespace colony
{
    class Ant : AI
    {
        public Ant(Terrain terrain)
        {
            // slightly above the ground, so that the World is not fighting with boundaries
            Z = 0.1f;
            IsSolid = false;
            Terrain = terrain;
            IsHoldingObject = false;
            RandomDirectionCount = 0;
            PreviousRandomMovement = new Movement();

            // create the directions and randomize the order
            Points = new engine.Common.Point[]
                {
                    new Point() { X = 0, Y = 0 }, // center
                    new Point() { X = 0 - (Width / 2), Y = 0 - (Height / 2) }, // top left
                    new Point() { X = 0 + (Width / 2), Y = 0 - (Height / 2) }, // top right
                    new Point() { X = 0 - (Width / 2), Y = 0 + (Height / 2) }, // bottom left
                    new Point() { X = 0 + (Width / 2), Y = 0 + (Height / 2) }, // bottom right
                };
            Directions = new DirectionType[]
                {
                    DirectionType.None,
                    DirectionType.Up,
                    DirectionType.Down,
                    DirectionType.Left,
                    DirectionType.Right
                };
            // randomize
            for (int i=0; i < Points.Length; i++)
            {
                var index = i;
                do
                {
                    // number between 0 and Directions.Length-1
                    index = (int)Math.Abs(Math.Floor((Utility.GetRandom(variance: Points.Length-1))));
                }
                while (index == i);

                // swap Points
                var tempP = Points[i];
                Points[i] = Points[index];
                Points[index] = tempP;

                // swap Directions
                var tempD = Directions[i];
                Directions[i] = Directions[index];
                Directions[index] = tempD;
            }
        }

        public PheromoneType Following { get; set; }
        public bool IsHoldingObject { get; set; }

        public override void Draw(IGraphics g)
        {
            RGBA color = RGBA.Black;
            switch(Following)
            {
                case PheromoneType.MoveDirt:
                    color = Red;
                    break;
                case PheromoneType.MoveQueen:
                    color = Purple;
                    break;
                case PheromoneType.MoveFood:
                    color = Green;
                    break;
                case PheromoneType.MoveEgg:
                    color = RGBA.White;
                    break;
                default:
                    throw new Exception("must have a pheromone to follow");
            }

            g.Rectangle(color, X - (Width / 2), Y - (Height / 2), Width, Height, fill: true, border: true, thickness: 1);

            if (IsHoldingObject)
            {
                g.Ellipse(RGBA.White, X, Y, Width / 2, Height / 2, fill: true, border: true, thickness: 1);
            }

            if (Following == PheromoneType.MoveQueen && IsInNest())
            {
                g.Text(RGBA.White, X, Y, "Nest", fontsize: 12f);
            }
        }

        public override ActionEnum Action(List<Element> elements, float angleToCenter, bool inZone, ref float xdelta, ref float ydelta, ref float zdelta, ref float angle)
        {
            // various ways to choose movement
            Movement move = default(Movement);
            DirectionType moveDirection = DirectionType.None;

            // map pheromone type to what is seeking and drop pheromones
            var dropPheromone = PheromoneType.None;
            var seekingBlock = BlockType.None;
            switch (Following)
            {
                case PheromoneType.MoveDirt:
                    dropPheromone = PheromoneType.DropDirt;
                    seekingBlock = BlockType.Dirt;
                    break;
                case PheromoneType.MoveQueen:
                    dropPheromone = PheromoneType.None;
                    seekingBlock = BlockType.None;
                    break;
                case PheromoneType.MoveFood:
                    dropPheromone = PheromoneType.DropFood;
                    seekingBlock = BlockType.Food;
                    break;
                case PheromoneType.MoveEgg:
                    dropPheromone = PheromoneType.DropEgg;
                    seekingBlock = BlockType.Egg;
                    break;
                default:
                    throw new Exception("unknown following pheromone type");
            }

            // check if we can drop the block
            if (IsHoldingObject)
            {
                // check the current block to see if we can drop
                if (Terrain.TryGetBlockDetails(X, Y, default(Movement), out BlockType block, out DirectionType[] pheromones))
                {
                    // drop the object
                    if (Terrain.TryChangeBlockDetails(X, Y, default(Movement), dropPheromone))
                    {
                        IsHoldingObject = false;
                    }
                } // TryGetBlockDetails
            } // IsHoldingObject

            // check if we should pick up a block
            if (!IsHoldingObject)
            {
                // determine the best path to follow - based on the pheromone trails
                if (!Terrain.TryGetBestMove(X, Y, Following, out bool[] directions)) throw new Exception("failed to get best move directions");
                move = ConvertDirectionsToMovement(directions, out moveDirection);

                // get the block details
                if (seekingBlock != BlockType.None)
                {
                    // check which corner we can pick up the block
                    if (TryFindBlockType(X, Y, move, Following, seekingBlock, out Point neighbor))
                    {
                        // pick up the block
                        if (Terrain.TryChangeBlockDetails(neighbor.X, neighbor.Y, move, Following))
                        {
                            IsHoldingObject = true;
                        }
                    } // TryGetMoveableBlock
                } // seeking a block type
            } // !IsHoldingObject

            // determine a path towards the drop zone
            if (IsHoldingObject)
            {
                // grab the best direction towards a drop zone
                if (!Terrain.TryGetBestMove(X, Y, dropPheromone, out bool[] directions)) throw new Exception("failed to get best move directions");
                move = ConvertDirectionsToMovement(directions, out moveDirection);
            }

            // move
            var tries = MaxMoveTries;
            do
            {
                if (Terrain.TryMove(X, Y, Width, Height, move))
                {
                    // calculate the angle
                    angle = engine.Common.Collision.CalculateAngleFromPoint(X, Y, X + move.dX, Y + move.dY);

                    // done
                    xdelta = move.dX;
                    ydelta = move.dY;
                    zdelta = 0f;
                    return ActionEnum.Move;
                }

                // on the first failed move, try to maneuver around the block
                if (tries == MaxMoveTries && moveDirection != DirectionType.None)
                {
                    move = AdjustMovementAroundBlock(moveDirection);
                }
                else
                {
                    // try random
                    RandomDirectionCount = 0;
                    move = GetRandomMovement();
                }
            }
            while (--tries > 0);

            // failed to move
            System.Diagnostics.Debug.WriteLine("failed to move");

            // no move
            xdelta = 0f;
            ydelta = 0f;
            zdelta = 0f;
            angle = 0f;
            return ActionEnum.None;
        }

        public override void Feedback(ActionEnum action, object item, bool result)
        {
            if (action == ActionEnum.Move && !result)
            {
                System.Diagnostics.Debug.WriteLine("the move was invalid");
            }
        }

        #region private
        private RGBA Red = new RGBA { R = 255, G = 0, B = 0, A = 255 };
        private RGBA Purple = new RGBA { R = 128, G = 0, B = 128, A = 255 };
        private RGBA Green = new RGBA { R = 0, G = 255, B = 0, A = 255 };
        private Point[] Points;
        private DirectionType[] Directions;
        private Terrain Terrain;
        private int RandomDirectionCount;
        private Movement PreviousRandomMovement;

        private const int MaxRandomDirectionCount = 16;
        private const int MaxMoveTries = 5;

        private Movement AdjustMovementAroundBlock(DirectionType direction)
        {
            // the ant tried to move and failed... depending on the direction it is trying to move, adjust to make the next move success
            //  eg. if trying to move down, check below and if we are move left we need to move more right in order to move down

            // get the current coordinates
            if (!Terrain.TryCoordinatesToRowColumn(X, Y, out int rowSrc, out int colSrc)) throw new Exception("failed to get coordinates");

            // there are always 2 directions to check
            Point corner1 = new Point();
            DirectionType moveD1 = DirectionType.None;
            Point corner2 = new Point();
            DirectionType moveD2 = DirectionType.None;
            Point destination = new Point();
            switch (direction)
            {
                case DirectionType.Up:
                    destination = new Point() { X = 0f, Y = -1f };
                    corner1 = new Point() { X = -0.5f, Y = 0f };
                    moveD1 = DirectionType.Left;
                    corner2 = new Point() { X = 0.5f, Y = 0f };
                    moveD2 = DirectionType.Right;
                    break;
                case DirectionType.Down:
                    destination = new Point() { X = 0f, Y = 1f };
                    corner1 = new Point() { X = -0.5f, Y = 0f };
                    moveD1 = DirectionType.Left;
                    corner2 = new Point() { X = 0.5f, Y = 0f };
                    moveD2 = DirectionType.Right;
                    break;
                case DirectionType.Left:
                    destination = new Point() { X = -1f, Y = 0f };
                    corner1 = new Point() { X = 0f, Y = -0.5f };
                    moveD1 = DirectionType.Up;
                    corner2 = new Point() { X = 0f, Y = 0.5f };
                    moveD2 = DirectionType.Down;
                    break;
                case DirectionType.Right:
                    destination = new Point() { X = 1f, Y = 0f };
                    corner1 = new Point() { X = 0f, Y = -0.5f };
                    moveD1 = DirectionType.Up;
                    corner2 = new Point() { X = 0f, Y = 0.5f };
                    moveD2 = DirectionType.Down;
                    break;
            }

            // get the coordinates of where we are trying to move
            if (!Terrain.TryCoordinatesToRowColumn(X + destination.X, Y + destination.Y, out int rowDst, out int colDst)) throw new Exception("failed to get coordinates");

            // check that the destination is valid
            if (!Terrain.TryGetBlockDetails(rowDst, colDst, out BlockType block, out DirectionType[] pheromones) && block != BlockType.Dirt)
            {
                throw new Exception("trying to move to an invalid block");
            }

            // determine which side of destination is 'blocking' us and move to adjust

            // get the coordinates of the first corner
            if (Terrain.TryCoordinatesToRowColumn(X + (Width * corner1.X), Y + (Height * corner1.Y), out int row1, out int col1))
            {
                if (row1 == rowSrc && col1 == colSrc)
                {
                    // move in this direction
                    return PheromoneDirectionToMovement(moveD1);
                }
            }

            // get the coordinates of the second corner
            if (Terrain.TryCoordinatesToRowColumn(X + (Width * corner2.X), Y + (Height * corner2.Y), out int row2, out int col2))
            {
                if (row2 == rowSrc && col2 == colSrc)
                {
                    // move in this direction
                    return PheromoneDirectionToMovement(moveD2);
                }
            }

            // nothing was successful
            throw new Exception("failed to adjust movement");
        }

        private Movement ConvertDirectionsToMovement(bool[] directions, out DirectionType moveDirection)
        {
            // ShortestPath returns a bool array with PheromoneDirectionType as the indices
            for(var i=0; i<Directions.Length; i++)
            {
                if (!directions[(int)Directions[i]]) continue;

                // check for diagonals
                if (((int)(Utility.GetRandom(variance: 1f) * 100) % 2) == 0)
                {
                    for (int j = i + 1; j < Directions.Length; j++)
                    {
                        if (!directions[(int)Directions[j]]) continue;

                        if (IsDiagonal(Directions[i], Directions[j]))
                        {
                            // diagonal
                            moveDirection = Directions[j];
                            return PheromoneDirectionToDiagonalMovement(Directions[i], Directions[j]);
                        }
                    }
                }

                // straight
                moveDirection = Directions[i];
                return PheromoneDirectionToMovement(Directions[i]);
            }

            // no direction determined, choose random
            moveDirection = DirectionType.None;
            return GetRandomMovement();
        }

        private bool IsDiagonal(DirectionType d1, DirectionType d2)
        {
            if ((d1 == DirectionType.Up || d1 == DirectionType.Down) && (d2 == DirectionType.Left || d2 == DirectionType.Right)) return true;
            if ((d1 == DirectionType.Left || d1 == DirectionType.Right) && (d2 == DirectionType.Up || d2 == DirectionType.Down)) return true;
            return true;
        }

        private Movement PheromoneDirectionToMovement(DirectionType direction)
        {
            var skitter = Utility.GetRandom(variance: 0.2f);
            var move = new Movement();
            switch (direction)
            {
                case DirectionType.Up:
                    move.dX = skitter;
                    move.dY = -1f + Math.Abs(skitter);
                    break;
                case DirectionType.Down:
                    move.dX = skitter;
                    move.dY = 1f - Math.Abs(skitter);
                    break;
                case DirectionType.Left:
                    move.dY = skitter;
                    move.dX = -1f + Math.Abs(skitter);
                    break;
                case DirectionType.Right:
                    move.dY = skitter;
                    move.dX = 1f - Math.Abs(skitter);
                    break;
            }

            // ensure the direction is valid
            var sum = (Math.Abs(move.dX) + Math.Abs(move.dY)) - 1f;
            if (sum > 0f) throw new Exception("invalid move");

            return move;
        }

        private Movement PheromoneDirectionToDiagonalMovement(DirectionType dir1, DirectionType dir2)
        {
            var skitter = Math.Abs(Utility.GetRandom(variance: 0.25f));
            var move = new Movement();
            switch (dir1)
            {
                case DirectionType.Up:
                    move.dY = -0.5f + skitter;
                    break;
                case DirectionType.Down:
                    move.dY = 0.5f - skitter;
                    break;
                case DirectionType.Left:
                    move.dX = -0.5f + skitter;
                    break;
                case DirectionType.Right:
                    move.dX = 0.5f - skitter;
                    break;
            }
            switch (dir2)
            {
                case DirectionType.Up:
                    move.dY = -0.5f - skitter;
                    break;
                case DirectionType.Down:
                    move.dY = 0.5f + skitter;
                    break;
                case DirectionType.Left:
                    move.dX = -0.5f - skitter;
                    break;
                case DirectionType.Right:
                    move.dX = 0.5f + skitter;
                    break;
            }

            // ensure the direction is valid
            var sum = (Math.Abs(move.dX) + Math.Abs(move.dY)) - 1f;
            if (sum > 0f) throw new Exception("invalid move");

            return move;
        }

        private bool TryFindBlockType(float x, float y, Movement move, PheromoneType pheromone, BlockType seekingBlock, out Point neighbor)
        {
            // look for a pheromone trail within the boundaries of the ant's hit box
            foreach (var pnt in Points)
            {
                // get the details about the block we are current on
                if (Terrain.TryGetBlockDetails(x + pnt.X, y + pnt.Y, move, out BlockType block, out DirectionType[] pheromones))
                {
                    if (block == seekingBlock && pheromones[(int)pheromone] != DirectionType.None)
                    {
                        neighbor = new Point()
                        {
                            X = x + pnt.X,
                            Y = y + pnt.Y
                        };
                        return true;
                    }
                }
            }

            // nothing
            neighbor = default(Point);
            return false;
        }

        private Movement GetRandomMovement()
        {
            // no pheromone trail, pick a random direction
            var move = new Movement();

            // once a direction is picked, go in that direction for a while
            if (--RandomDirectionCount > 0)
            {
                move.dX = PreviousRandomMovement.dX;
                move.dY = PreviousRandomMovement.dY;
                return move;
            }

            // choose a random direction
            var drive = Utility.GetRandom(variance: 0.8f);
            if ((int)Math.Ceiling(Utility.GetRandom(variance: 100f)) % 2 == 0)
            {
                move.dX = drive;
                if (drive < 0) move.dY = 1f - Math.Abs(drive);
                else move.dY = -1f + Math.Abs(drive);
            }
            else
            {
                move.dY = drive;
                if (drive < 0) move.dX = 1f - Math.Abs(drive);
                else move.dX = -1f + Math.Abs(drive);
            }

            // ensure the direction is valid
            var sum = (Math.Abs(move.dX) + Math.Abs(move.dY)) - 1f;
            if (sum > 0f) throw new Exception("invalid move");

            // retain this direction
            RandomDirectionCount = MaxRandomDirectionCount;
            PreviousRandomMovement.dX = move.dX;
            PreviousRandomMovement.dY = move.dY;
            return move;
        }

        private bool IsInNest()
        {
            // must be a Queen
            if (Following != PheromoneType.MoveQueen) return false;

            // check our surrounds and follow these rules
            // 1. must be at the end of a MoveQueen trail (eg. on MoveQueen and all adjacent blocks point to the current block)
            // 2. must be underground (eg. ???)

            if (!Terrain.TryCoordinatesToRowColumn(X, Y, out int row, out int col)) return false;

            // 1. pheromone trails
            BlockType block;
            DirectionType[] pheromones;
            if (Terrain.TryGetBlockDetails(row, col, out block, out pheromones))
            {
                if (pheromones[(int)PheromoneType.MoveQueen] == DirectionType.None) return false;
            }
            if (!IsViableNestBlock(row + 1, col, PheromoneType.MoveQueen, DirectionType.Up)) return false;
            if (!IsViableNestBlock(row - 1, col, PheromoneType.MoveQueen, DirectionType.Down)) return false;
            if (!IsViableNestBlock(row, col + 1, PheromoneType.MoveQueen, DirectionType.Left)) return false;
            if (!IsViableNestBlock(row, col - 1, PheromoneType.MoveQueen, DirectionType.Right)) return false;

            // 2. underground - look up until you find a dirt block
            for (int r = row; r >= 0; r--)
            {
                if (Terrain.TryGetBlockDetails(r, col, out block, out pheromones) && block == BlockType.Dirt) return true;
            }

            return false;
        }

        private bool IsViableNestBlock(int row, int col, PheromoneType pheromone, DirectionType oppositeDirection)
        {
            if (Terrain.TryGetBlockDetails(row, col, out BlockType block, out DirectionType[] pheromones))
            {
                if (pheromones[(int)pheromone] == DirectionType.None) { }
                else if (pheromones[(int)pheromone] != oppositeDirection) return false;
            }

            return true;
        }

        #endregion
    }
}
