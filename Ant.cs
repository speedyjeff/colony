using System;
using System.Collections.Generic;
using engine.Common;
using engine.Common.Entities;
using engine.Common.Entities.AI;

// todo - speed is not taken into account for any of the movements (eg. IsMoveable)

namespace colony
{
    class Ant : AI
    {
        public Ant(Terrain terrain)
        {
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
            Directions = new PheromoneDirectionType[]
                {
                    PheromoneDirectionType.None,
                    PheromoneDirectionType.Up,
                    PheromoneDirectionType.Down,
                    PheromoneDirectionType.Left,
                    PheromoneDirectionType.Right
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
            g.Rectangle(Body, X - (Width / 2), Y - (Height / 2), Width, Height, fill: true, border: true, thickness: 1);

            if (IsHoldingObject)
            {
                g.Ellipse(RGBA.White, X, Y, Width / 2, Height / 2, fill: true, border: true, thickness: 1);
            }
        }

        public override ActionEnum Action(List<Element> elements, float angleToCenter, bool inZone, ref float xdelta, ref float ydelta, ref float zdelta, ref float angle)
        {
            // various ways to choose movement
            Movement move = default(Movement);
            PheromoneDirectionType moveDirection = PheromoneDirectionType.None;

            // determine if we should drop the object, or move towards a place where we can drop the object
            var dropPheromone = PheromoneType.None;
            if (Following == PheromoneType.MoveDirt) dropPheromone = PheromoneType.DropDirt;
            else throw new Exception("must have a pheromone to follow on where to drop");

            // determine what block type we are seeking
            var seekingBlock = BlockType.None;
            if (Following == PheromoneType.MoveDirt) seekingBlock = BlockType.Dirt;
            else throw new Exception("must have a pheromone to follow");

            // check if we can drop the block or move towards a drop zone
            if (IsHoldingObject)
            {
                // check the current block to see if we can drop
                if (Terrain.TryGetBlockDetails(X, Y, default(Movement), out BlockType block, out PheromoneDirectionType[] pheromones))
                {
                    // check if we are on a drop pheromone and it is open
                    if (pheromones[(int)dropPheromone] != PheromoneDirectionType.None)
                    {
                        // drop the object
                        if (Terrain.TrySetBlockDetails(X, Y, default(Movement), BlockType.Dirt))
                        {
                            IsHoldingObject = false;
                        }
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
                if (TryGetMoveableBlock(X, Y, move, Following, out Point neighbor, out BlockType block))
                {
                    // check what the block would be in our new location
                    if (block == seekingBlock)
                    {
                        // pick up the block
                        if (Terrain.TrySetBlockDetails(neighbor.X, neighbor.Y, move, BlockType.Air))
                        {
                            IsHoldingObject = true;
                        }
                    } // Following == MoveDirt
                } // TryGetMoveableBlock
            } // !IsHoldingObject

            // determine a path towards the drop zone
            if (IsHoldingObject)
            {
                //moveTowardsDrop = FollowDropPheromone(X, Y, dropPheromone);
                // grab the best direction towards a drop zone
                if (!Terrain.TryGetBestMove(X, Y, dropPheromone, out bool[] directions)) throw new Exception("failed to get best move directions");
                move = ConvertDirectionsToMovement(directions, out moveDirection);
            }

            // move
            var tries = MaxMoveTries;
            do
            {
                if (IsValidMove(X, Y, move))
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
                if (tries == MaxMoveTries && moveDirection != PheromoneDirectionType.None)
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
        private RGBA Body = new RGBA { R = 255, G = 0, B = 0, A = 255 };
        private Point[] Points;
        private PheromoneDirectionType[] Directions;
        private Terrain Terrain;
        private int RandomDirectionCount;
        private Movement PreviousRandomMovement;

        private const int MaxRandomDirectionCount = 16;
        private const int MaxMoveTries = 5;

        private Movement AdjustMovementAroundBlock(PheromoneDirectionType direction)
        {
            // the ant tried to move and failed... depending on the direction it is trying to move, adjust to make the next move success
            //  eg. if trying to move down, check below and if we are move left we need to move more right in order to move down

            // get the current coordinates
            if (!Terrain.TryCoordinatesToRowColumn(X, Y, out int rowSrc, out int colSrc)) throw new Exception("failed to get coordinates");

            // there are always 2 directions to check
            Point corner1 = new Point();
            PheromoneDirectionType moveD1 = PheromoneDirectionType.None;
            Point corner2 = new Point();
            PheromoneDirectionType moveD2 = PheromoneDirectionType.None;
            Point destination = new Point();
            switch (direction)
            {
                case PheromoneDirectionType.Up:
                    destination = new Point() { X = 0f, Y = -1f };
                    corner1 = new Point() { X = -0.5f, Y = 0f };
                    moveD1 = PheromoneDirectionType.Left;
                    corner2 = new Point() { X = 0.5f, Y = 0f };
                    moveD2 = PheromoneDirectionType.Right;
                    break;
                case PheromoneDirectionType.Down:
                    destination = new Point() { X = 0f, Y = 1f };
                    corner1 = new Point() { X = -0.5f, Y = 0f };
                    moveD1 = PheromoneDirectionType.Left;
                    corner2 = new Point() { X = 0.5f, Y = 0f };
                    moveD2 = PheromoneDirectionType.Right;
                    break;
                case PheromoneDirectionType.Left:
                    destination = new Point() { X = -1f, Y = 0f };
                    corner1 = new Point() { X = 0f, Y = -0.5f };
                    moveD1 = PheromoneDirectionType.Up;
                    corner2 = new Point() { X = 0f, Y = 0.5f };
                    moveD2 = PheromoneDirectionType.Down;
                    break;
                case PheromoneDirectionType.Right:
                    destination = new Point() { X = 1f, Y = 0f };
                    corner1 = new Point() { X = 0f, Y = -0.5f };
                    moveD1 = PheromoneDirectionType.Up;
                    corner2 = new Point() { X = 0f, Y = 0.5f };
                    moveD2 = PheromoneDirectionType.Down;
                    break;
            }

            // get the coordinates of where we are trying to move
            if (!Terrain.TryCoordinatesToRowColumn(X + destination.X, Y + destination.Y, out int rowDst, out int colDst)) throw new Exception("failed to get coordinates");

            // check that the destination is valid
            if (!Terrain.TryGetBlockDetails(rowDst, colDst, out BlockType block, out PheromoneDirectionType[] pheromones) ||
                Terrain.IsBlocking(block))
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

        private Movement ConvertDirectionsToMovement(bool[] directions, out PheromoneDirectionType moveDirection)
        {
            // ShortestPath returns a bool array with PheromoneDirectionType as the indices
            foreach(var dir in Directions)
            {
                if (directions[(int)dir])
                {
                    moveDirection = dir;
                    return PheromoneDirectionToMovement(dir);
                }
            }

            // no direction determined, choose random
            moveDirection = PheromoneDirectionType.None;
            return GetRandomMovement();
        }

        private Movement PheromoneDirectionToMovement(PheromoneDirectionType direction)
        {
            var skitter = Utility.GetRandom(variance: 0.2f);
            var move = new Movement();
            switch (direction)
            {
                case PheromoneDirectionType.Up:
                    move.dX = skitter;
                    move.dY = -1f + Math.Abs(skitter);
                    break;
                case PheromoneDirectionType.Down:
                    move.dX = skitter;
                    move.dY = 1f - Math.Abs(skitter);
                    break;
                case PheromoneDirectionType.Left:
                    move.dY = skitter;
                    move.dX = -1f + Math.Abs(skitter);
                    break;
                case PheromoneDirectionType.Right:
                    move.dY = skitter;
                    move.dX = 1f - Math.Abs(skitter);
                    break;
            }

            // ensure the direction is valid
            var sum = (Math.Abs(move.dX) + Math.Abs(move.dY)) - 1f;
            if (sum > 0f) throw new Exception("invalid move");

            return move;
        }

        private bool TryGetMoveableBlock(float x, float y, Movement move, PheromoneType pheromone, out Point neighbor, out BlockType block)
        {
            // look for a pheromone trail within the boundaries of the ant's hit box
            foreach (var pnt in Points)
            {
                // get the details about the block we are current on
                if (Terrain.TryGetBlockDetails(x + pnt.X, y + pnt.Y, move, out block, out PheromoneDirectionType[] pheromones))
                {
                    if (IsMoveable(block) && pheromones[(int)pheromone] != PheromoneDirectionType.None)
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
            block = BlockType.None;
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

        private bool IsMoveable(BlockType block)
        {
            if (!IsHoldingObject && Terrain.IsMoveable(block))
            {
                // check that we can move it
                if ((Following == PheromoneType.MoveDirt && block == BlockType.Dirt))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsValidMove(float x, float y, Movement move)
        {
            // check all 4 corners
            foreach (var pnt in Points)
            {
                // get the block type
                if (Terrain.TryGetBlockDetails(x + pnt.X, y + pnt.Y, move, out BlockType block, out PheromoneDirectionType[] pheromones))
                {
                    // check the block type
                    if (Terrain.IsBlocking(block)) return false;
                }
                else
                {
                    // out of bounds
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
