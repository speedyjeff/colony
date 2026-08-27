using System;
using System.Collections.Generic;
using engine.Common;
using engine.Common.Entities;
using engine.Common.Entities.AI;

// todo
//  save & load - build demos

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
            FoodCounter = 0;
            Age = (int)Math.Abs(Utility.GetRandom(1f) * BlockConstants.AntAdultAge);
            WanderAngle = Utility.GetRandom(variance: 180f);

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
            for (int i = 0; i < Points.Length; i++)
            {
                var index = i;
                do
                {
                    // number between 0 and Directions.Length-1
                    index = (int)Math.Abs(Math.Floor((Utility.GetRandom(variance: Points.Length - 1))));
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
        public bool IsEgg { get; set; }
        public int FoodCounter { get; private set; }
        public float TimerCounter { get; private set; } // [0.0..1.0]
        public int Age { get; private set; }

        public override void Draw(IGraphics g)
        {
            // get details for the Ant
            RGBA color = RGBA.Black;
            var antSizeFactor = 1f;
            switch (Following)
            {
                case PheromoneType.MoveDirt:
                    color = Red;
                    break;
                case PheromoneType.MoveQueen:
                    color = Purple;
                    antSizeFactor = 2f;
                    break;
                case PheromoneType.MoveFood:
                    color = Green;
                    antSizeFactor = 0.75f;
                    break;
                case PheromoneType.MoveEgg:
                    color = RGBA.White;
                    break;
                case PheromoneType.None:
                    color = RGBA.Black;
                    break;
                case PheromoneType.MoveDeadAnt:
                    color = Rust;
                    break;
                default:
                    throw new Exception("must have a pheromone to follow");
            }

            if (IsEgg)
            {
                // note - same code is in Terrain.Draw
                var x = X - (Width / 2);
                var y = Y - (Height / 2);
                var eggWidth = Terrain.BlockWidth / 2;
                var eggHeight = Terrain.BlockHeight / 2;
                // series of small ellipses in the shape of a 3 segment egg
                g.Ellipse(color, x + (eggWidth / 2), y + (eggHeight / 4), (eggWidth / 2), (eggHeight / 3), fill: true, border: true);
                g.Ellipse(color, x + (eggWidth / 4), y + (eggHeight / 3), (eggWidth / 2), (eggHeight / 3), fill: true, border: true);
                g.Ellipse(color, x, y + (eggHeight / 4), (eggWidth / 2), (eggHeight / 3), fill: true, border: true);
            }
            else
            {
                // create the ant image
                if (AntImages == null) CreateAntImages(g, color);

                // display that the Queen Ant is in the nest and can lay eggs
                if (Following == PheromoneType.MoveQueen && IsInNest())
                {
                    g.Ellipse(PaleYellow, X - (Width / 2), Y - (Height / 2), Math.Max(Width, Height), Math.Max(Width, Height), fill: true, border: false);
                }

                // scale width and height based on age (scale by 1/2 to full antSizeFactor)
                var midFactor = (antSizeFactor / 2f);
                var antWidth = (Width * (antSizeFactor - midFactor)) + (Width * (midFactor * ((float)Age / (float)BlockConstants.AntAdultAge)));
                var antHeight = (Height * (antSizeFactor - midFactor)) + (Height * (midFactor * ((float)Age / (float)BlockConstants.AntAdultAge)));
                antHeight *= 1f + ((float)Math.Sin(GaitPhase) * 0.025f);

                // draw the ant (using 3 points of the parallelogram)
                var antImages = AntImages ?? throw new InvalidOperationException("ant images were not initialized");
                var bounds = RotateAntBoundingBox(antWidth, antHeight, Angle + ((float)Math.Sin(GaitPhase) * 2.5f));
                g.Image(antImages[((int)(GaitPhase / Math.PI)) & 1], bounds);

                // draw what the ant is holding
                if (IsHoldingObject)
                {
                    var angleRadians = Angle * (Math.PI / 180f);
                    var objectX = X + ((float)Math.Sin(angleRadians) * antHeight * 0.42f) - (antWidth / 10f);
                    var objectY = Y - ((float)Math.Cos(angleRadians) * antHeight * 0.42f) - (antHeight / 10f);
                    if (Following == PheromoneType.MoveDirt)
                    {
                        g.Rectangle(Brown, objectX, objectY, antWidth / 5, antHeight / 5, fill: true, border: true, thickness: 1);
                    }
                    else if (Following == PheromoneType.MoveEgg || Following == PheromoneType.MoveFood || Following == PheromoneType.MoveDeadAnt)
                    {
                        g.Ellipse(color, objectX, objectY, antWidth / 5, antHeight / 5, fill: true, border: true, thickness: 1);
                    }
                    else if (Following == PheromoneType.MoveQueen)
                    {
                        // nothing, it will be done eating soon
                    }
                }
            }

            // progress bar
            if (TimerCounter > 0 && TimerCounter <= 1f)
            {
                // bar fill
                g.Rectangle(Yellow, X - (Width / 2), Y + (Height * 0.8f), Width * (TimerCounter <= 1f ? TimerCounter : 1f), Height / 8, fill: true, border: false);
                // border
                g.Rectangle(RGBA.Black, X - (Width / 2), Y + (Height * 0.8f), Width, Height / 8, fill: false, border: true, thickness: 1f);
            }
        }

        public override void Update()
        {
            // update age
            if (Age < BlockConstants.AntMaxAge) Age++;

            // Queen laying Eggs
            if (Following == PheromoneType.MoveQueen)
            {
                // check if in the nest and if we can/should provide a new egg
                if (IsInNest())
                {
                    // check if we have eaten enough food
                    if (FoodCounter >= BlockConstants.QueenFull && TimerCounter >= 1f)
                    {
                        // check if there is an available block for an egg
                        if (Terrain.TryCoordinatesToRowColumn(X, Y, out int row, out int col))
                        {
                            // todo - use the randomized list?

                            // try to lay the egg in  a neighboring block
                            if (Terrain.TryChangeBlockDetails(row + 1, col, Following) ||
                                Terrain.TryChangeBlockDetails(row - 1, col, Following) ||
                                Terrain.TryChangeBlockDetails(row, col + 1, Following) ||
                                Terrain.TryChangeBlockDetails(row, col - 1, Following)) 
                            {
                                // reset the food counter
                                FoodCounter = 0;
                            }
                        }
                    }
                }
            }

            // Queen eating and digesting
            if (Following == PheromoneType.MoveQueen)
            {
                // check if we are ready to eat
                if (FoodCounter == 0) TimerCounter = 1.1f;

                // check if eating
                if (IsHoldingObject)
                {
                    // eat
                    FoodCounter++;

                    // no longer holder the food
                    IsHoldingObject = false;

                    // reset digestion
                    TimerCounter = 0f;
                }

                // digesting the food
                TimerCounter += (1f / (float)BlockConstants.QueenDigest);
            }

            // Egg choosing what to follow
            if (IsEgg)
            {
                // increment the hatch timer
                TimerCounter += (1f / (float)BlockConstants.EggHatch);

                // if no pheromone assigned, choose one randomly
                if (Following == PheromoneType.None)
                {
                    // choose a random pheromone
                    var rand = (int)Math.Abs(Math.Floor(Utility.GetRandom(variance: 10)));
                    if (rand >= 0 && rand < 5) Following = PheromoneType.MoveDirt;
                    else if (rand >= 5 && rand < 7) Following = PheromoneType.MoveEgg;
                    else if (rand == 7) Following = PheromoneType.MoveQueen;
                    else if (rand == 8) Following = PheromoneType.MoveDeadAnt;
                    else Following = PheromoneType.MoveFood;
                }

                // choose what type of pheromone to follow based on pheromones on this block
                if (Terrain.TryGetBlockDetails(X, Y, default(Movement), out BlockType block, out int count, out DirectionType[] pheromones))
                {
                    // choose a pheromone and assign to the Egg
                    var setPheromone = PheromoneType.None;
                    if (pheromones[(int)PheromoneType.MoveDirt] != DirectionType.None) setPheromone = PheromoneType.MoveDirt;
                    else if (pheromones[(int)PheromoneType.MoveEgg] != DirectionType.None) setPheromone = PheromoneType.MoveEgg;
                    else if (pheromones[(int)PheromoneType.MoveFood] != DirectionType.None) setPheromone = PheromoneType.MoveFood;
                    else if (pheromones[(int)PheromoneType.MoveQueen] != DirectionType.None) setPheromone = PheromoneType.MoveQueen;
                    else if (pheromones[(int)PheromoneType.MoveDeadAnt] != DirectionType.None) setPheromone = PheromoneType.MoveDeadAnt;

                    // set the pheromone to follow and remove the pheromone
                    if (setPheromone != PheromoneType.None)
                    {
                        Following = setPheromone;
                        Terrain.TryClearPheromone(X, Y, setPheromone);
                    }
                }
            }

            // Eggs hatching
            if (IsEgg && TimerCounter >= 1f)
            {
                // hatch the egg
                IsEgg = false;
                TimerCounter = 0;
                Age = 0;

                // add the pheromone back to this spot for another egg
                Terrain.TryApplyPheromone(X, Y, PheromoneType.DropEgg);
            }

            // death?
            if (Age >= BlockConstants.AntMaxAge)
            {
                // todo - what if holding something

                // die
                if (Terrain.TryChangeBlockDetails(X, Y, default(Movement), PheromoneType.DeadAnt))
                {
                    // change health to 0 and it will die
                    Health = 0;
                    IsDead = true;
                }
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
                    // check if ready to eat
                    if (TimerCounter >= 1f && FoodCounter < BlockConstants.QueenFull) seekingBlock = BlockType.Food; // to eat
                    else seekingBlock = BlockType.None; // in the process of eating (eg. mouth is full)
                    break;
                case PheromoneType.MoveFood:
                    dropPheromone = PheromoneType.DropFood;
                    seekingBlock = BlockType.Food;
                    break;
                case PheromoneType.MoveEgg:
                    dropPheromone = PheromoneType.DropEgg;
                    seekingBlock = BlockType.Egg;
                    break;
                case PheromoneType.MoveDeadAnt:
                    dropPheromone = PheromoneType.DropDeadAnt;
                    seekingBlock = BlockType.DeadAnt;
                    break;
                case PheromoneType.None:
                    dropPheromone = PheromoneType.None;
                    seekingBlock = BlockType.None;
                    break;
                default:
                    throw new Exception("unknown following pheromone type");
            }

            // check if we can drop the block
            if (IsHoldingObject)
            {
                // check the current block to see if we can drop
                if (Terrain.TryGetBlockDetails(X, Y, default(Movement), out BlockType block, out int count, out DirectionType[] pheromones))
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

            // eggs do not move
            if (IsEgg)
            {
                // no move
                xdelta = 0f;
                ydelta = 0f;
                zdelta = 0f;
                angle = 0f;
                return ActionEnum.None;
            }

            // move
            var tries = MaxMoveTries;
            do
            {
                var steeredMove = SteerTowards(move, tries == MaxMoveTries ? NormalTurnRate : AvoidanceTurnRate);
                if (Terrain.TryMove(X, Y, Width, Height, steeredMove))
                {
                    // calculate the angle
                    angle = engine.Common.Collision.CalculateAngleFromPoint(X, Y, X + steeredMove.dX, Y + steeredMove.dY);

                    // done
                    xdelta = steeredMove.dX;
                    ydelta = steeredMove.dY;
                    zdelta = 0f;
                    PreviousMovement = steeredMove;
                    GaitPhase += 0.72f;
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
        private static RGBA Red = new RGBA { R = 119, G = 57, B = 38, A = 255 };
        private static RGBA Purple = new RGBA { R = 102, G = 62, B = 92, A = 255 };
        private static RGBA Green = new RGBA { R = 68, G = 87, B = 49, A = 255 };
        private static RGBA Brown = new RGBA { R = 98, G = 65, B = 42, A = 255 };
        private static RGBA Yellow = new RGBA { R = 209, G = 169, B = 75, A = 255 };
        private static RGBA PaleYellow = new RGBA { R = 209, G = 169, B = 75, A = 80 };
        private static RGBA Transparent = new RGBA { R = 1, G = 2, B = 3, A = 255 };
        private static RGBA Rust = new RGBA { R = 101, G = 58, B = 43, A = 255 };
        private Point[] Points;
        private DirectionType[] Directions;
        private Terrain Terrain;
        private int RandomDirectionCount;
        private Movement PreviousRandomMovement;
        private IImage[]? AntImages;
        private Point[]? AntBoundingBox;
        private Movement PreviousMovement;
        private float WanderAngle;
        private float GaitPhase;

        private const int MaxRandomDirectionCount = 5;
        private const int MaxMoveTries = 5;
        private const float NormalTurnRate = 0.24f;
        private const float AvoidanceTurnRate = 0.58f;

        private void CreateAntImages(IGraphics g, RGBA color)
        {
            if (AntImages != null) throw new Exception("must be called only once");

            AntImages = new IImage[2];
            for (var frame = 0; frame < AntImages.Length; frame++)
            {
                AntImages[frame] = CreateAntImage(g, color, frame == 0 ? -1f : 1f);
            }
        }

        private IImage CreateAntImage(IGraphics g, RGBA color, float stride)
        {

            // top is forward (eg. Angle 0 is facing up)

            // create the Ant image
            var antImage = g.CreateImage((int)Width, (int)Height);
            antImage.Graphics.Clear(Transparent);

            // draw the Ant
            var thickness = 1f;

            // head
            var headWidth = Width / 2f;
            var headHeight = Height / 6f;
            
            // first segment
            var bodyWidth = Width * 0.5f;
            var bodyHeight = Height / 3f;
            var bodyYOffset = headHeight / 4f;

            // second segment
            var segmentWidth = Width * 0.7f;
            var segmentHeight = Height / 4f;
            var segmentYOffset = bodyHeight / 4f;

            // legs
            var legLength = Width * 0.22f;
            var legThickness = 2f;

            // wings
            var wingWidth = Width / 4f;
            var wingHeight = Height / 2f;

            // center
            var x = Width / 2f;
            // Draw ant head (ellipse)
            antImage.Graphics.Ellipse(color, x - (headWidth / 2), 1f, headWidth, headHeight, fill: true, border: true, thickness);

            // Draw ant body segments (three ellipses)
            var thoraxY = headHeight + bodyYOffset;
            var abdomenY = thoraxY + bodyHeight + segmentYOffset;
            antImage.Graphics.Ellipse(color, x - bodyWidth / 2, thoraxY, bodyWidth, bodyHeight, fill: true, border: true, thickness);
            antImage.Graphics.Ellipse(color, x - segmentWidth / 2, abdomenY, segmentWidth, segmentHeight, fill: true, border: true, thickness);

            // Draw ant legs (lines)
            // Top segment legs
            antImage.Graphics.Line(color, x - bodyWidth / 2, thoraxY + bodyHeight * 0.2f, x - bodyWidth / 2 - legLength, thoraxY + (legLength * 0.25f * stride), legThickness);
            antImage.Graphics.Line(color, x + bodyWidth / 2, thoraxY + bodyHeight * 0.2f, x + bodyWidth / 2 + legLength, thoraxY - (legLength * 0.25f * stride), legThickness);

            // Middle segment legs
            antImage.Graphics.Line(color, x - segmentWidth / 2, abdomenY, x - segmentWidth / 2 - legLength, abdomenY + (legLength * 0.2f * stride), legThickness);
            antImage.Graphics.Line(color, x + segmentWidth / 2, abdomenY, x + segmentWidth / 2 + legLength, abdomenY - (legLength * 0.2f * stride), legThickness);

            // Bottom segment legs
            var rearLegY = abdomenY + segmentHeight * 0.65f;
            antImage.Graphics.Line(color, x - segmentWidth / 2, rearLegY, x - segmentWidth / 2 - legLength, rearLegY - (legLength * 0.3f * stride), legThickness);
            antImage.Graphics.Line(color, x + segmentWidth / 2, rearLegY, x + segmentWidth / 2 + legLength, rearLegY + (legLength * 0.3f * stride), legThickness);

            if (Following == PheromoneType.MoveQueen)
            {
                // Draw wings (ellipses)
                antImage.Graphics.Ellipse(new RGBA { R = 219, G = 215, B = 194, A = 170 }, x - (bodyWidth / 2) - (wingWidth / 2), thoraxY, wingWidth, wingHeight, fill: true, border: false);
                antImage.Graphics.Ellipse(new RGBA { R = 219, G = 215, B = 194, A = 170 }, x + (bodyWidth / 2) - (wingWidth / 2), thoraxY, wingWidth, wingHeight, fill: true, border: false);
            }

            // Draw ant antennae (lines)
            antImage.Graphics.Line(color, x - headWidth / 4, headHeight * 0.15f, x - headWidth / 2, 0, legThickness);
            antImage.Graphics.Line(color, x + headWidth / 4, headHeight * 0.15f, x + headWidth / 2, 0, legThickness);

            // remove the background
            antImage.MakeTransparent(Transparent);
            return antImage;
        }

        private Point[] RotateAntBoundingBox(float width, float height, float angle)
        {
            if (AntBoundingBox == null) AntBoundingBox = new engine.Common.Point[3];

            // calculate the four corners of the bounding box before rotation

            // upper-left
            AntBoundingBox[0] = new Point() { X = X - width / 2, Y = Y - height / 2 };
            // upper-right
            AntBoundingBox[1] = new Point() { X = X + width / 2, Y = Y - height / 2 };
            // lower-left
            AntBoundingBox[2] = new Point() { X = X - width / 2, Y = Y + height / 2 };

            // convert angle from degrees to radians
            var rads = (float)(angle * (Math.PI / 180));

            // rotate each point around {X,Y}
            for (var i = 0; i < AntBoundingBox.Length; i++)
            {
                var cosTheta = (float)Math.Cos(rads);
                var sinTheta = (float)Math.Sin(rads);

                var dx = AntBoundingBox[i].X - X;
                var dy = AntBoundingBox[i].Y - Y;

                AntBoundingBox[i].X = (cosTheta * dx) - (sinTheta * dy) + X;
                AntBoundingBox[i].Y = (sinTheta * dx) + (cosTheta * dy) + Y;
            }

            return AntBoundingBox;
        }

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
            if (!Terrain.TryGetBlockDetails(rowDst, colDst, out BlockType block, out int count, out DirectionType[] pheromones) && block != BlockType.Dirt)
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
            for (var i = 0; i < Directions.Length; i++)
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
            return false;
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
                if (Terrain.TryGetBlockDetails(x + pnt.X, y + pnt.Y, move, out BlockType block, out int count, out DirectionType[] pheromones))
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
            if (--RandomDirectionCount <= 0)
            {
                WanderAngle += Utility.GetRandom(variance: 28f);
                RandomDirectionCount = MaxRandomDirectionCount;
            }

            var move = new Movement();
            Collision.CalculateLineByAngle(
                x: 0,
                y: 0,
                WanderAngle,
                distance: 0.9f,
                out _,
                out _,
                out move.dX,
                out move.dY);

            PreviousRandomMovement = NormalizeMovement(move);
            return PreviousRandomMovement;
        }

        private Movement SteerTowards(Movement desired, float turnRate)
        {
            desired = NormalizeMovement(desired);
            if (PreviousMovement.IsDefault()) return desired;

            var movement = new Movement
            {
                dX = PreviousMovement.dX + ((desired.dX - PreviousMovement.dX) * turnRate),
                dY = PreviousMovement.dY + ((desired.dY - PreviousMovement.dY) * turnRate)
            };

            return NormalizeMovement(movement);
        }

        private static Movement NormalizeMovement(Movement movement)
        {
            var total = Math.Abs(movement.dX) + Math.Abs(movement.dY);
            if (total <= 0f) return movement;

            movement.dX = (movement.dX / total) * 0.9f;
            movement.dY = (movement.dY / total) * 0.9f;
            return movement;
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
            if (Terrain.TryGetBlockDetails(row, col, out block, out int count, out pheromones))
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
                if (Terrain.TryGetBlockDetails(r, col, out block, out count, out pheromones) && block == BlockType.Dirt) return true;
            }

            return false;
        }

        private bool IsViableNestBlock(int row, int col, PheromoneType pheromone, DirectionType oppositeDirection)
        {
            if (Terrain.TryGetBlockDetails(row, col, out BlockType block, out int count, out DirectionType[] pheromones))
            {
                if (pheromones[(int)pheromone] == DirectionType.None) { }
                else if (pheromones[(int)pheromone] != oppositeDirection) return false;
            }

            return true;
        }

        #endregion
    }
}
