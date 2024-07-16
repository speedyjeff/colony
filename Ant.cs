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
            Directions = new engine.Common.Point[]
                {
                    new Point() { X = 0, Y = 0 }, // center
                    new Point() { X = 0 - (Width / 2), Y = 0 - (Height / 2) }, // top left
                    new Point() { X = 0 + (Width / 2), Y = 0 - (Height / 2) }, // top right
                    new Point() { X = 0 - (Width / 2), Y = 0 + (Height / 2) }, // bottom left
                    new Point() { X = 0 + (Width / 2), Y = 0 + (Height / 2) }, // bottom right
                };
            // randomize
            for(int i=0; i < Directions.Length; i++)
            {
                var temp = Directions[i];
                var index = i;
                do
                {
                    // number between 0 and Directions.Length-1
                    index = (int)Math.Abs(Math.Floor((Utility.GetRandom(variance: Directions.Length-1))));
                }
                while (index == i);
                Directions[i] = Directions[index];
                Directions[index] = temp;
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
            Movement moveTowardsDrop = default(Movement);
            Movement pheromoneMove = default(Movement);

            // check if we can drop the block or move towards a drop zone
            if (IsHoldingObject)
            {
                // determine if we should drop the object, or move towards a place where we can drop the object
                var dropPheromone = PheromoneType.None;
                if (Following == PheromoneType.MoveDirt) dropPheromone = PheromoneType.DropDirt;
                else throw new Exception("must have a pheromone to follow on where to drop");

                // check the current block to see if we can drop
                if (Terrain.TryGetBlockDetails(X, Y, out BlockType block, out PheromoneDirectionType[] pheromones))
                {
                    // check if we are on a drop pheromone and it is open
                    if (pheromones[(int)dropPheromone] != PheromoneDirectionType.None)
                    {
                        // drop the object
                        if (Terrain.TrySetBlockDetails(X, Y, BlockType.Dirt))
                        {
                            IsHoldingObject = false;
                        }
                    }
                } // TryGetBlockDetails

                // determine a path towards the drop zone
                if (IsHoldingObject)
                {
                    moveTowardsDrop = FollowDropPheromone(X, Y, dropPheromone);
                }
            } // IsHoldingObject

            // check to see if we have a pheromone to following
            var seekingBlock = BlockType.None;
            if (Following == PheromoneType.MoveDirt) seekingBlock = BlockType.Dirt;
            else throw new Exception("must have a pheromone to follow");
            pheromoneMove = FollowPheromone(Following, X, Y);

            // check if we should pick up a block
            if (!IsHoldingObject)
            { 
                // check if we are following a pheromone trail and could pick up a block
                if (!pheromoneMove.IsDefault())
                {
                    // get the block details
                    if (TryGetMoveableBlock(X + pheromoneMove.dX, Y + pheromoneMove.dY, out Point neighbor, out BlockType block))
                    {
                        // check what the block would be in our new location
                        if (block == seekingBlock)
                        {
                            // pick up the block
                            if (Terrain.TrySetBlockDetails(neighbor.X, neighbor.Y, BlockType.Air))
                            {
                                IsHoldingObject = true;

                                // flip the direction (move against the pheromone trail)
                                pheromoneMove.FlipDirection();
                            }
                        } // Following == MoveDirt
                    } // TryGetMoveableBlock
                } // is valid direction
            } // !IsHoldingObject
            else
            {
                // flip the pheromone trail
                pheromoneMove.FlipDirection();
            }

            // precedence
            //  1. If holding an object
            //       move towards a drop pheromone
            //  2. move towards a pheromone trail   
            //  3. move in one of the directions
            //  4. random

            //System.Diagnostics.Debug.WriteLine($"{X},{Y}");

            // move
            var moves = new List<Movement>();
            if (IsHoldingObject && !moveTowardsDrop.IsDefault()) moves.Add(moveTowardsDrop);
            if (!pheromoneMove.IsDefault()) moves.Add(pheromoneMove);
            foreach(var pnt in Directions)
            {
                if (pnt.X == 0 && pnt.Y == 0) continue;
                moves.Add(PointToMovement(pnt));
            }
            moves.Add(GetRandomMovement());
            foreach (var move in moves)
            {
                // try to move
                if (IsValidMove(X + move.dX, Y + move.dY))
                {
                    // calculate the angle
                    angle = engine.Common.Collision.CalculateAngleFromPoint(X, Y, X + move.dX, Y + move.dY);

                    // debug
                    //System.Diagnostics.Debug.WriteLine($"{X},{Y} {X + move.dX},{Y + move.dY} {angle} {move.dX},{move.dY} {IsHoldingObject} {Following} {pheromoneMove.dX},{pheromoneMove.dY} {moveTowardsDrop.dX},{moveTowardsDrop.dY}");

                    // done
                    xdelta = move.dX;
                    ydelta = move.dY;
                    zdelta = 0f;
                    return ActionEnum.Move;
                }
            }

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
        private Point[] Directions;
        private Terrain Terrain;
        private int RandomDirectionCount;
        private Movement PreviousRandomMovement;

        private const int MaxRandomDirectionCount = 32;

        private Movement FollowDropPheromone(float x, float y, PheromoneType pheromone)
        {
            // check if we are on a drop pheromone
            // within a 3x block radius
            for (var multiplier = 6; multiplier>1; multiplier-=2)
            {
                // look through in each direction looking for this pheromone
                foreach (var pnt in Directions)
                {
                    if (pnt.X == 0 && pnt.Y == 0) continue;
                    
                    // get the details about the block we are current on
                    if (Terrain.TryGetBlockDetails(x + (pnt.X * multiplier), y + (pnt.Y * multiplier), out BlockType block, out PheromoneDirectionType[] pheromones))
                    {
                        // check the pheromone
                        if (pheromones[(int)pheromone] != PheromoneDirectionType.None)
                        {
                            // set the movement in this direction
                            return PointToMovement(pnt);
                        }
                    }
                }
            }

            // did not find one
            return default(Movement);
        }

        private Movement PointToMovement(Point pnt)
        {
            Movement move;
            if (pnt.X < 0 && pnt.Y < 0) move = new Movement() { dX = -0.5f, dY = -0.5f };
            else if (pnt.X < 0 && pnt.Y > 0) move = new Movement() { dX = -0.5f, dY = 0.5f };
            else if (pnt.X > 0 && pnt.Y < 0) move = new Movement() { dX = 0.5f, dY = -0.5f };
            else if (pnt.X > 0 && pnt.Y > 0) move = new Movement() { dX = 0.5f, dY = 0.5f };
            else throw new Exception("invalid move");

            // add skitter
            var skitter = Math.Abs(Utility.GetRandom(variance: 0.2f));
            if (((int)Utility.GetRandom(variance: 100f) % 2) == 0)
            {
                // dX increase
                if (move.dX < 0) move.dX -= skitter;
                else move.dX += skitter;
                if (move.dY < 0) move.dY += skitter;
                else move.dY -= skitter;
            }
            else
            {
                // dY increase
                if (move.dY < 0) move.dY -= skitter;
                else move.dY += skitter;
                if (move.dX < 0) move.dX += skitter;
                else move.dX -= skitter;
            }

            // ensure the direction is valid
            var sum = (Math.Abs(move.dX) + Math.Abs(move.dY)) - 1f;
            if (sum > 0f) throw new Exception("invalid move");

            return move;
        }

        private Movement FollowPheromone(PheromoneType pheromone, float x, float y)
        {
            // look for a pheromone trail within the boundaries of the ant's hit box
            foreach (var pnt in Directions)
            {
                // get the details about the block we are current on
                if (Terrain.TryGetBlockDetails(x + pnt.X, y + pnt.Y, out BlockType block, out PheromoneDirectionType[] pheromones))
                {
                    // pick a direction based on the pheromone trail
                    Movement move =  PheromoneDirectionToMovement(pheromones[(int)pheromone]);
                    if (!move.IsDefault())
                    {       // choose this path
                            RandomDirectionCount = 0;
                            return move;
                    }
                }
            }

            // no pheromone trail
            return default(Movement);
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

        private bool TryGetMoveableBlock(float x, float y, out Point point, out BlockType block)
        {
            // look for a pheromone trail within the boundaries of the ant's hit box
            foreach (var pnt in Directions)
            {
                // get the details about the block we are current on
                if (Terrain.TryGetBlockDetails(x + pnt.X, y + pnt.Y, out block, out PheromoneDirectionType[] pheromones))
                {
                    if (IsMoveable(block))
                    {
                        point = new Point()
                        {
                            X = x + pnt.X,
                            Y = y + pnt.Y
                        };
                        return true;
                    }
                }
            }

            point = default(Point);
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

        private bool IsValidMove(float x, float y)
        {
            // check all 4 corners
            foreach (var pnt in Directions)
            {
                // get the block type
                if (Terrain.TryGetBlockDetails(x + pnt.X, y + pnt.Y, out BlockType block, out PheromoneDirectionType[] pheromones))
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
