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
            Z = 0.1f;
            IsSolid = false;
            Terrain = terrain;
            IsHoldingObject = false;
            RandomDirectionCount = 0;
            Angle = 0f;
            PreviousRandomMovement = new Movement();
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
            // determine the direction
            var move = CalculateMovement(X, Y, Width, Height, out bool isRandomMove);

            // when to drop the object?
            if (isRandomMove && IsHoldingObject && Following == PheromoneType.MoveDirt)
            {
                // drop the object
                if (Terrain.TrySetBlockDetails(X, Y, Width, Height, BlockType.Dirt))
                {
                    IsHoldingObject = false;
                }
            }

            // try to move
            if (IsValidMove(X + move.dX, Y + move.dY, Width, Height))
            {
                // calculate the angle
                Angle = engine.Common.Collision.CalculateAngleFromPoint(X, Y, X + move.dX, Y + move.dY);

                // done
                xdelta = move.dX;
                ydelta = move.dY;
                zdelta = 0f;
                angle = Angle;
                return ActionEnum.Move;
            }

            // check if we can pick up a block
            if (!IsHoldingObject)
            {
                // get the block details
                if (TryGetMoveableBlock(X + move.dX, Y + move.dY, Width, Height, out Point neighbor, out BlockType block))
                {
                    // check what the block would be in our new location
                    if (Following == PheromoneType.MoveDirt && block == BlockType.Dirt)
                    {
                        // pick up the block
                        if (Terrain.TrySetBlockDetails(neighbor.X, neighbor.Y, BlockType.Air))
                        {
                            IsHoldingObject = true;
                        }
                    }
                }
            }

            // try to randomly move again
            RandomDirectionCount = 0;
            move = GetRandomMovement();

            // calculate the angle
            Angle = engine.Common.Collision.CalculateAngleFromPoint(X, Y, X + move.dX, Y + move.dY);

            // done
            xdelta = move.dX;
            ydelta = move.dY;
            zdelta = 0f;
            angle = Angle;
            return ActionEnum.Move;
        }

        public override void Feedback(ActionEnum action, object item, bool result)
        {
            if (action == ActionEnum.Move && !result)
            {
                System.Diagnostics.Debug.WriteLine("failed to move");
            }
        }

        #region private
        private RGBA Body = new RGBA { R = 255, G = 0, B = 0, A = 255 };
        private Terrain Terrain;
        private float Angle;
        private int RandomDirectionCount;
        private Movement PreviousRandomMovement;

        private const int MaxRandomDirectionCount = 10;

        private Movement CalculateMovement(float x, float y, float width, float height, out bool isRandomMove)
        {
            // init
            Movement move = default(Movement);
            isRandomMove = false;

            // look for a pheromone trail within the boundaries of the ant's hit box
            foreach (var pnt in new engine.Common.Point[]
                {
                    new Point() { X = x, Y = Y }, // center
                    new Point() { X = x - (width / 2), Y = y - (height / 2) }, // top left
                    new Point() { X = x + (width / 2), Y = y - (height / 2) }, // top right
                    new Point() { X = x - (width / 2), Y = y + (height / 2) }, // bottom left
                    new Point() { X = x + (width / 2), Y = y + (height / 2) }, // bottom right
                })
            {
                // get the details about the block we are current on
                if (Terrain.TryGetBlockDetails(pnt.X, pnt.Y, out BlockType block, out PheromoneDirectionType[] pheromones))
                {
                    // pick a direction based on the pheromone trail
                    if (TryFollowPheromone(pheromones, out move))
                    {       // choose this path
                            RandomDirectionCount = 0;
                            return move;
                    }
                }
            }

            // choose a direction randomly
            isRandomMove = true;
            return GetRandomMovement();
        }

        private bool TryGetMoveableBlock(float x, float y, float width, float height, out Point point, out BlockType block)
        {
            // look for a pheromone trail within the boundaries of the ant's hit box
            foreach (var pnt in new engine.Common.Point[]
                {
                    new Point() { X = x, Y = y }, // center
                    new Point() { X = x - (width / 2), Y = y - (height / 2) }, // top left
                    new Point() { X = x + (width / 2), Y = y - (height / 2) }, // top right
                    new Point() { X = x - (width / 2), Y = y + (height / 2) }, // bottom left
                    new Point() { X = x + (width / 2), Y = y + (height / 2) }, // bottom right
                })
            {
                // get the details about the block we are current on
                if (Terrain.TryGetBlockDetails(pnt.X, pnt.Y, out block, out PheromoneDirectionType[] pheromones))
                {
                    if (IsMoveable(block))
                    {
                        point = pnt;
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
            var skitter = Utility.GetRandom(variance: 0.2f);

            // once a direction is picked, go in that direction for a while
            if (--RandomDirectionCount > 0)
            {
                move.dX = PreviousRandomMovement.dX;
                move.dY = PreviousRandomMovement.dY;
                return move;
            }

            // choose a random direction
            if ((int)Math.Ceiling(Utility.GetRandom(variance: 1f) * 100) % 2 == 0)
            {
                move.dY = skitter;
                move.dX = (Utility.GetRandom(variance: 0.4f) * 2);
            }
            else
            {
                move.dX = skitter;
                move.dY = Utility.GetRandom(variance: 0.4f) * 2;
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

        private bool TryFollowPheromone(PheromoneDirectionType[] pheromones, out Movement move)
        {
            // init
            move = new Movement();

            // determine the right move based on Pheromones
            var skitter = Utility.GetRandom(variance: 0.2f);
            switch (pheromones[(int)Following])
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
                default:
                    return false;
            }

            // if IsHolding - go the opposite direction of what was chosen
            if (IsHoldingObject)
            {
                move.dX *= -1;
                move.dY *= -1;
            }

            // ensure the direction is valid
            var sum = (Math.Abs(move.dX) + Math.Abs(move.dY)) - 1f;
            if (sum > 0f) throw new Exception("invalid move");

            return true;
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

        private bool IsValidMove(float x, float y, float width, float height)
        {
            // check all 4 corners
            foreach (var pnt in new engine.Common.Point[]
            {
                new engine.Common.Point() { X = x - width / 2, Y = y - height / 2 },
                new engine.Common.Point() { X = x + width / 2, Y = y - height / 2 },
                new engine.Common.Point() { X = x - width / 2, Y = y + height / 2 },
                new engine.Common.Point() { X = x + width / 2, Y = y + height / 2 }
            })
            {
                // get the block type
                if (Terrain.TryGetBlockDetails(pnt.X, pnt.Y, out BlockType block, out PheromoneDirectionType[] pheromones))
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
