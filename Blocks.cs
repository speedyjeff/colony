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
            for (int r = 0; r < Terrain.Rows; r++)
            {
                // initialize
                DirtColors[r] = new RGBA[Terrain.Columns];
                AirColors[r] = new RGBA[Terrain.Columns];
                FoodColors[r] = new RGBA[Terrain.Columns];
                DeadAntColors[r] = new RGBA[Terrain.Columns];

                // set the values
                for (int c = 0; c < Terrain.Columns; c++)
                {
                    // dirt
                    var rand = Utility.GetRandom(variance: 0.2f);
                    DirtColors[r][c] = new RGBA
                    {
                        R = (byte)(BrownColor.R + (BrownColor.R * rand)),
                        G = (byte)(BrownColor.G + (BrownColor.G * rand)),
                        B = (byte)(BrownColor.B + (BrownColor.B * rand)),
                        A = 255
                    };
                    // air
                    rand = Utility.GetRandom(variance: 0.02f);
                    AirColors[r][c] = new RGBA
                    {
                        R = (byte)(WhiteColor.R + (WhiteColor.R * rand)),
                        G = (byte)(WhiteColor.G + (WhiteColor.G * rand)),
                        B = (byte)(WhiteColor.B + (WhiteColor.B * rand)),
                        A = 50
                    };
                    // food
                    rand = Utility.GetRandom(variance: 0.4f);
                    FoodColors[r][c] = new RGBA
                    {
                        R = (byte)(GreenColor.R + (GreenColor.R * rand)),
                        G = (byte)(GreenColor.G + (GreenColor.G * rand)),
                        B = (byte)(GreenColor.B + (GreenColor.B * rand)),
                        A = 50
                    };
                    // dead ant
                    rand = Utility.GetRandom(variance: 0.2f);
                    DeadAntColors[r][c] = new RGBA
                    {
                        R = (byte)(RustColor.R + (RustColor.R * rand)),
                        G = (byte)(RustColor.G + (RustColor.G * rand)),
                        B = (byte)(RustColor.B + (RustColor.B * rand)),
                        A = 50
                    };
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
                        case BlockType.WasteDirt:
                        case BlockType.Dirt:
                            g.Rectangle(DirtColors[r][c], x, y, Terrain.BlockWidth, Terrain.BlockHeight, fill: true, border: false);
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
            g.Rectangle(PurpleColor, 0 - (Width / 2), 0 - (Height / 2), Width, Height, fill: false, border: true, thickness: 2f);
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
        private RGBA BrownColor = new RGBA { R = 139, G = 69, B = 19, A = 255 };
        private RGBA PurpleColor = new RGBA { R = 128, G = 0, B = 128, A = 255 };
        private RGBA WhiteColor = new RGBA { R = 250, G = 250, B = 250, A = 255 };
        private RGBA RedColor = new RGBA { R = 255, G = 0, B = 0, A = 255 };
        private RGBA GreenColor = new RGBA { R = 0, G = 255, B = 0, A = 255 };
        private RGBA RustColor = new RGBA { R = 210, G = 105, B = 30, A = 255 };
        private Terrain Terrain;
        private PheromoneType ActivePheromone;

        private void DisplayDropPheromone(IGraphics g, RGBA color, DirectionType dir, float x, float y)
        {
            if (dir == DirectionType.None) return;

            // display drop Pheromone
            g.Ellipse(color, x + Terrain.BlockWidth / 2, y + Terrain.BlockHeight / 2, Terrain.BlockWidth / 4, Terrain.BlockHeight / 4, fill: true, border: false);
        }

        private void DisplayMovePheromone(IGraphics g, RGBA color, DirectionType dir, float x, float y)
        {
            if (dir == DirectionType.None) return;

            // display the direction of the Pheromone
            switch (dir)
            {
                case DirectionType.Up:
                    g.Triangle(color,
                        x + Terrain.BlockWidth / 2, y,
                        x + Terrain.BlockWidth, y + Terrain.BlockHeight / 2,
                        x, y + Terrain.BlockHeight / 2, fill: true, border: false);
                    break;
                case DirectionType.Down:
                    g.Triangle(color,
                        x + Terrain.BlockWidth / 2, y + Terrain.BlockHeight,
                        x + Terrain.BlockWidth, y + Terrain.BlockHeight / 2,
                        x, y + Terrain.BlockHeight / 2, fill: true, border: false);
                    break;
                case DirectionType.Left:
                    g.Triangle(color,
                        x, y + Terrain.BlockHeight / 2,
                        x + Terrain.BlockWidth / 2, y,
                        x + Terrain.BlockWidth / 2, y + Terrain.BlockHeight, fill: true, border: false);
                    break;
                case DirectionType.Right:
                    g.Triangle(color,
                        x + Terrain.BlockWidth, y + Terrain.BlockHeight / 2,
                        x + Terrain.BlockWidth / 2, y,
                        x + Terrain.BlockWidth / 2, y + Terrain.BlockHeight, fill: true, border: false);
                    break;
                default:
                    throw new Exception("invalid direction");
            }
        }
        #endregion
    }
}
