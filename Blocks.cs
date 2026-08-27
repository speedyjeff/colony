using System;
using engine.Common;
using engine.Common.Entities;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace colony
{
    class Blocks : Obstacle
    {
        public Blocks(Terrain terrain)
        {
            IsSolid = true;
            Width = terrain.Width;
            Height = terrain.Height;
            Terrain = terrain;
            ActivePheromone = PheromoneType.None;

            // set the dirt chunk colors
            DirtColors = new RGBA[Terrain.Rows][];
            AirColors = new RGBA[Terrain.Rows][];
            FoodColors = new RGBA[Terrain.Rows][];
            DeadAntColors = new RGBA[Terrain.Rows][];
            WasteColors = new RGBA[Terrain.Rows][];
            for (int r = 0; r < Terrain.Rows; r++)
            {
                // initialize
                DirtColors[r] = new RGBA[Terrain.Columns];
                AirColors[r] = new RGBA[Terrain.Columns];
                FoodColors[r] = new RGBA[Terrain.Columns];
                DeadAntColors[r] = new RGBA[Terrain.Columns];
                WasteColors[r] = new RGBA[Terrain.Columns];

                // set the values
                for (int c = 0; c < Terrain.Columns; c++)
                {
                    // dirt
                    var rand = Utility.GetRandom(variance: 0.16f);
                    DirtColors[r][c] = Vary(BrownColor, rand, 255);
                    WasteColors[r][c] = Vary(WasteColor, rand, 255);
                    // air
                    rand = Utility.GetRandom(variance: 0.08f);
                    AirColors[r][c] = Vary(AirColor, rand, 255);
                    // food
                    rand = Utility.GetRandom(variance: 0.22f);
                    FoodColors[r][c] = Vary(FoodColor, rand, 255);
                    // dead ant
                    rand = Utility.GetRandom(variance: 0.14f);
                    DeadAntColors[r][c] = Vary(RustColor, rand, 255);
                }
            }
        }

        public override void Draw(IGraphics g)
        {
            // draw the dirt chunks
            for (int r = 0; r < Terrain.Rows; r++)
            {
                for (int c = 0; c < Terrain.Columns; c++)
                {
                    // convert to x,y
                    var x = (X - Width / 2) + (c * Terrain.BlockWidth);
                    var y = (Y - Height / 2) + (r * Terrain.BlockHeight);

                    // get the block details
                    if (!Terrain.TryGetBlockDetails(r, c, out BlockType type, out int count, out DirectionType[] pheromones)) throw new Exception("invalid block details");

                    // display the block
                    switch (type)
                    {
                        case BlockType.Air:
                            g.Rectangle(AirColors[r][c], x, y, Terrain.BlockWidth, Terrain.BlockHeight, fill: true, border: false);
                            break;
                        case BlockType.Dirt:
                            g.Rectangle(DirtColors[r][c], x, y, Terrain.BlockWidth, Terrain.BlockHeight, fill: true, border: false);
                            break;
                        case BlockType.WasteDirt:
                            g.Rectangle(WasteColors[r][c], x, y, Terrain.BlockWidth, Terrain.BlockHeight, fill: true, border: false);
                            break;
                        case BlockType.Food:
                            // note - this is based on FoodFull being 4
                            if (BlockConstants.FoodFull != 4) throw new Exception("invalid food full");
                            // background
                            g.Rectangle(AirColors[r][c], x, y, Terrain.BlockWidth, Terrain.BlockHeight, fill: true, border: false);
                            // stem - the shape of a small x
                            g.Line(DirtColors[r][c], x + (Terrain.BlockWidth / 4), y + (Terrain.BlockHeight / 4), x + ((3 * Terrain.BlockWidth) / 4), y + ((3 * Terrain.BlockHeight) / 4), thickness: 2f);
                            g.Line(DirtColors[r][c], x + ((3 * Terrain.BlockWidth) / 4), y + (Terrain.BlockHeight / 4), x + (Terrain.BlockWidth / 4), y + ((3 * Terrain.BlockHeight) / 4), thickness: 2f);
                            // 4 fruit
                            var fcolor = FoodColors[r][c];
                            if (count >= 1)
                            {
                                if (c - 1 > 0) fcolor = FoodColors[r][c - 1];
                                g.Ellipse(fcolor, x + (Terrain.BlockWidth / 8), y, (Terrain.BlockWidth / 2), (Terrain.BlockHeight / 2), fill: true, border: false);
                            }
                            if (count >= 2)
                            {
                                if (r - 1 > 0) fcolor = FoodColors[r - 1][c];
                                g.Ellipse(fcolor, x + (Terrain.BlockWidth / 8), y + (Terrain.BlockHeight / 2), (Terrain.BlockWidth / 2), Terrain.BlockHeight / 2, fill: true, border: false);
                            }
                            if (count >= 3)
                            {
                                if (c + 1 < Terrain.Columns) fcolor = FoodColors[r][c + 1];
                                g.Ellipse(fcolor, x + (Terrain.BlockWidth / 2), y + (Terrain.BlockHeight / 10), (Terrain.BlockWidth / 2), (Terrain.BlockHeight / 2), fill: true, border: false);
                            }
                            if (count >= 4)
                            {
                                if (r + 1 < Terrain.Rows) fcolor = FoodColors[r + 1][c];
                                g.Ellipse(fcolor, x + (Terrain.BlockWidth / 2), y + (Terrain.BlockHeight / 2) + (Terrain.BlockHeight / 10), (Terrain.BlockWidth / 2), (Terrain.BlockHeight / 2), fill: true, border: false);
                            }
                            break;
                        case BlockType.Egg:
                            // note - same code is in Ant.Draw
                            var eggWidth = Terrain.BlockWidth/2;
                            var eggHeight = Terrain.BlockHeight/2;
                            // background
                            g.Rectangle(AirColors[r][c], x, y, Terrain.BlockWidth, Terrain.BlockHeight, fill: true, border: false);
                            // series of small ellipses in the shape of a 3 segment egg
                            g.Ellipse(RGBA.White, x + (eggWidth / 2), y + (eggHeight / 4), (eggWidth / 2), (eggHeight / 3), fill: true, border: true);
                            g.Ellipse(RGBA.White, x + (eggWidth / 4), y + (eggHeight / 3), (eggWidth / 2), (eggHeight / 3), fill: true, border: true);
                            g.Ellipse(RGBA.White, x, y + (eggHeight / 4), (eggWidth / 2), (eggHeight / 3), fill: true, border: true);
                            break;
                        case BlockType.DeadAnt:
                            // background
                            g.Rectangle(AirColors[r][c], x, y, Terrain.BlockWidth, Terrain.BlockHeight, fill: true, border: false);
                            // 3 small horizonal ellipses
                            var thickness = 1f;
                            for (int i = 0; i < count; i++)
                            {
                                // dead ant
                                g.Ellipse(DeadAntColors[r][c], x + (Terrain.BlockWidth / 4), y + (Terrain.BlockHeight / 4), (Terrain.BlockWidth / 6), (Terrain.BlockHeight / 8), fill: true, border: true, thickness);
                                g.Ellipse(DeadAntColors[r][c], x + (Terrain.BlockWidth / 5), y + (Terrain.BlockHeight / 3), (Terrain.BlockWidth / 6), (Terrain.BlockHeight / 8), fill: true, border: true, thickness);
                                g.Ellipse(DeadAntColors[r][c], x + (Terrain.BlockWidth / 4), y + (Terrain.BlockHeight * 0.45f), (Terrain.BlockWidth / 6), (Terrain.BlockHeight / 8), fill: true, border: true, thickness);
                                // shift for slight overlap
                                x += Terrain.BlockWidth / 10;
                                y += Terrain.BlockHeight / 10;
                            }
                            break;
                        default:
                            throw new Exception("invalid dirt state");
                    }

                    // pheromones
                    switch(ActivePheromone)
                    {
                        case PheromoneType.None:
                            // valid
                            break;
                        case PheromoneType.MoveDirt:
                            DisplayMovePheromone(g, RedColor, pheromones[(int)PheromoneType.MoveDirt], x, y);
                            break;
                            case PheromoneType.DropDirt:
                            DisplayDropPheromone(g, RedColor, pheromones[(int)PheromoneType.DropDirt], x, y);
                            break;
                        case PheromoneType.MoveQueen:
                            DisplayMovePheromone(g, PurpleColor, pheromones[(int)PheromoneType.MoveQueen], x, y);
                            break;
                        case PheromoneType.MoveFood:
                            DisplayMovePheromone(g, GreenColor, pheromones[(int)PheromoneType.MoveFood], x, y);
                            break;
                        case PheromoneType.DropFood:
                            DisplayDropPheromone(g, GreenColor, pheromones[(int)PheromoneType.DropFood], x, y);
                            break;
                        case PheromoneType.MoveEgg:
                            DisplayMovePheromone(g, WhiteColor, pheromones[(int)PheromoneType.MoveEgg], x, y);
                            break;
                        case PheromoneType.DropEgg:
                            DisplayDropPheromone(g, WhiteColor, pheromones[(int)PheromoneType.DropEgg], x, y);
                            break;
                        case PheromoneType.MoveDeadAnt:
                            DisplayMovePheromone(g, RustColor, pheromones[(int)PheromoneType.MoveDeadAnt], x, y);
                            break;
                        case PheromoneType.DropDeadAnt:
                            DisplayDropPheromone(g, RustColor, pheromones[(int)PheromoneType.DropDeadAnt], x, y);
                            break;
                        default:
                            throw new Exception("invalid pheromone");
                    }
                }
            }

            // draw the rim
            g.Rectangle(RimColor, 0 - (Width / 2), 0 - (Height / 2), Width, Height, fill: false, border: true, thickness: 2f);
        }

        public void SetActivePheromone(PheromoneType type)
        {
            // set the active pheromone
            ActivePheromone = type;
        }

        #region private
        private RGBA[][] DirtColors;
        private RGBA[][] AirColors;
        private RGBA[][] FoodColors;
        private RGBA[][] DeadAntColors;
        private RGBA[][] WasteColors;
        private static readonly RGBA BrownColor = new RGBA { R = 92, G = 61, B = 42, A = 255 };
        private static readonly RGBA WasteColor = new RGBA { R = 119, G = 80, B = 53, A = 255 };
        private static readonly RGBA AirColor = new RGBA { R = 35, G = 39, B = 40, A = 255 };
        private static readonly RGBA FoodColor = new RGBA { R = 117, G = 145, B = 72, A = 255 };
        private static readonly RGBA PurpleColor = new RGBA { R = 174, G = 126, B = 181, A = 190 };
        private static readonly RGBA WhiteColor = new RGBA { R = 225, G = 211, B = 174, A = 190 };
        private static readonly RGBA RedColor = new RGBA { R = 196, G = 112, B = 72, A = 190 };
        private static readonly RGBA GreenColor = new RGBA { R = 122, G = 164, B = 92, A = 190 };
        private static readonly RGBA RustColor = new RGBA { R = 151, G = 91, B = 61, A = 255 };
        private static readonly RGBA RimColor = new RGBA { R = 104, G = 91, B = 73, A = 255 };
        private Terrain Terrain;
        private PheromoneType ActivePheromone;

        private static RGBA Vary(RGBA color, float variation, byte alpha)
        {
            return new RGBA
            {
                R = Clamp(color.R + (color.R * variation)),
                G = Clamp(color.G + (color.G * variation)),
                B = Clamp(color.B + (color.B * variation)),
                A = alpha
            };
        }

        private static byte Clamp(float value)
        {
            return (byte)Math.Max(0f, Math.Min(255f, value));
        }

        private void DisplayDropPheromone(IGraphics g, RGBA color, DirectionType dir, float x, float y)
        {
            if (dir == DirectionType.None) return;

            // display drop Pheromone
            g.Ellipse(color, x + (Terrain.BlockWidth * 0.38f), y + (Terrain.BlockHeight * 0.38f), Terrain.BlockWidth * 0.24f, Terrain.BlockHeight * 0.24f, fill: false, border: true, thickness: 2f);
        }

        private void DisplayMovePheromone(IGraphics g, RGBA color, DirectionType dir, float x, float y)
        {
            if (dir == DirectionType.None) return;

            // display the direction of the Pheromone
            switch (dir)
            {
                case DirectionType.Up:
                    g.Line(color, x + Terrain.BlockWidth * 0.25f, y + Terrain.BlockHeight * 0.62f, x + Terrain.BlockWidth * 0.5f, y + Terrain.BlockHeight * 0.35f, 2f);
                    g.Line(color, x + Terrain.BlockWidth * 0.75f, y + Terrain.BlockHeight * 0.62f, x + Terrain.BlockWidth * 0.5f, y + Terrain.BlockHeight * 0.35f, 2f);
                    break;
                case DirectionType.Down:
                    g.Line(color, x + Terrain.BlockWidth * 0.25f, y + Terrain.BlockHeight * 0.38f, x + Terrain.BlockWidth * 0.5f, y + Terrain.BlockHeight * 0.65f, 2f);
                    g.Line(color, x + Terrain.BlockWidth * 0.75f, y + Terrain.BlockHeight * 0.38f, x + Terrain.BlockWidth * 0.5f, y + Terrain.BlockHeight * 0.65f, 2f);
                    break;
                case DirectionType.Left:
                    g.Line(color, x + Terrain.BlockWidth * 0.62f, y + Terrain.BlockHeight * 0.25f, x + Terrain.BlockWidth * 0.35f, y + Terrain.BlockHeight * 0.5f, 2f);
                    g.Line(color, x + Terrain.BlockWidth * 0.62f, y + Terrain.BlockHeight * 0.75f, x + Terrain.BlockWidth * 0.35f, y + Terrain.BlockHeight * 0.5f, 2f);
                    break;
                case DirectionType.Right:
                    g.Line(color, x + Terrain.BlockWidth * 0.38f, y + Terrain.BlockHeight * 0.25f, x + Terrain.BlockWidth * 0.65f, y + Terrain.BlockHeight * 0.5f, 2f);
                    g.Line(color, x + Terrain.BlockWidth * 0.38f, y + Terrain.BlockHeight * 0.75f, x + Terrain.BlockWidth * 0.65f, y + Terrain.BlockHeight * 0.5f, 2f);
                    break;
                default:
                    throw new Exception("invalid direction");
            }
        }
        #endregion
    }
}
