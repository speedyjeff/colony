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
            for (int r = 0; r < Terrain.Rows; r++)
            {
                // initialize
                DirtColors[r] = new RGBA[Terrain.Columns];
                AirColors[r] = new RGBA[Terrain.Columns];

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
                    if (!Terrain.TryGetBlockDetails(r, c, out var type, out var pheromones)) throw new Exception("invalid block details");

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
                        default:
                            throw new Exception("invalid dirt state");
                    }

                    // pheromones
                    if (ActivePheromone == PheromoneType.MoveDirt)
                    {
                        DisplayMovePheromone(g, RedColor, pheromones[(int)PheromoneType.MoveDirt], x, y);
                    }
                    else if (ActivePheromone == PheromoneType.DropDirt)
                    {
                        DisplayDropPheromone(g, RedColor, pheromones[(int)PheromoneType.DropDirt], x, y);
                    }
                    else if (ActivePheromone == PheromoneType.MoveQueen)
                    {
                        DisplayMovePheromone(g, PurpleColor, pheromones[(int)PheromoneType.MoveQueen], x, y);
                    }
                    else if (ActivePheromone != PheromoneType.None)
                    {
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
        private RGBA BrownColor = new RGBA { R = 139, G = 69, B = 19, A = 50 };
        private RGBA PurpleColor = new RGBA { R = 128, G = 0, B = 128, A = 50 };
        private RGBA WhiteColor = new RGBA { R = 250, G = 250, B = 250, A = 50 };
        private RGBA RedColor = new RGBA { R = 255, G = 0, B = 0, A = 50 };
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
