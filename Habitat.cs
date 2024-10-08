using System;
using System.Windows.Forms;
using engine;
using engine.Winforms;
using engine.Common;
using engine.Common.Entities;
using Microsoft.AspNetCore.SignalR.Client;

// architecture
//
// UI                                   | Game logic     | Entities
// -------------------------------------|----------------|----------------
//  World                               |  Terrain       |  Ant
//   - Handles UI screen drawing        |   - Game logic |   - Egg
//  Controls                            |                |   - Queen
//   - Handles UI controls              |                |  Blocks
//  Habitat                             |                |  
//   - Handles mouse and keyboard input |                |
//  Camera                              |                |
//   - Handles camera movement          |                |

namespace colony
{
    public partial class Habitat : Form
    {
        public Habitat()
        {
            InitializeComponent();

            // set title
            this.Name = "colony";
            this.Text = "colony";
            // set the window size
            this.Width = 1500;
            this.Height = 1500;
            // setting a double buffer eliminates the flicker
            this.DoubleBuffered = true;

            // basic background
            var width = 10000;
            var height = 800;
            var background = new Background(width, height) { GroundColor = new RGBA { R = 100, G = 100, B = 100, A = 255 }, BasePace = 2f };

            // initial the terrain blocks
            //TerrainGenerator.SplitInHalf(rows: 100, columns: 100, out BlockDetails[][] scene, out PlayerDetails[] playerDets);
            //TerrainGenerator.BigEmpty(out BlockDetails[][] scene, out PlayerDetails[] playerDets);
            //TerrainGenerator.DemoRound(rows: 100, columns: 100, out BlockDetails[][] scene, out PlayerDetails[] playerDets);
            TerrainGenerator.Demo(rows: 100, columns: 100, out BlockDetails[][] scene, out PlayerDetails[] playerDets);

            // init
            MouseButton = engine.Common.MouseButton.None;
            CurrentPheromone = PheromoneType.None;
            Terrain = new Terrain(width: 100 * scene[0].Length, height: 100 * scene.Length, scene);
            Terrain.Speed = background.BasePace * Constants.Speed;

            if (Terrain.Speed > (Terrain.BlockWidth / 2) ||
                Terrain.Speed > (Terrain.BlockHeight / 2)) throw new Exception("speed is too fast for the terrain");

            // add blocks
            var blocks = new Blocks(Terrain) { X = 0, Y = 0 };

            // add the camera and starting ants
            Camera = new Camera() { Name = "camera", X = 0, Y = 0 };
            var players = new Player[1 + playerDets.Length];
            players[0] = Camera;
            for (int i = 0; i < playerDets.Length; i++)
            {
                players[i + 1] = CreateAnt(Terrain, playerDets[i].X, playerDets[i].Y, playerDets[i].Pheromone );
            }

            // create the HUD
            Hud = new Controls();
            Hud.AddControl(PheromoneType.MoveDirt, new RGBA() { R = 255, G = 0, B = 0, A = 255 }, "dirt");
            Hud.AddControl(PheromoneType.DropDirt, new RGBA() { R = 255, G = 0, B = 0, A = 100 }, "drop");
            Hud.AddControl(PheromoneType.MoveFood, new RGBA() { R = 0, G = 255, B = 0, A = 255 }, "food");
            Hud.AddControl(PheromoneType.DropFood, new RGBA() { R = 0, G = 255, B = 0, A = 100 }, "drop");
            Hud.AddControl(PheromoneType.MoveEgg, new RGBA() { R = 255, G = 255, B = 255, A = 255 }, "egg");
            Hud.AddControl(PheromoneType.DropEgg, new RGBA() { R = 255, G = 255, B = 255, A = 100 }, "drop");
            Hud.AddControl(PheromoneType.MoveDeadAnt, new RGBA() { R = 210, G = 105, B = 30, A = 200 }, "dead");
            Hud.AddControl(PheromoneType.DropDeadAnt, new RGBA() { R = 210, G = 105, B = 30, A = 100 }, "drop");
            Hud.AddControl(PheromoneType.MoveQueen, new RGBA() { R = 128, G = 0, B = 128, A = 255 }, "queen");

            // create the world
            World = new World(
              new WorldConfiguration()
              {
                  Width = width,
                  Height = height,
                  EnableZoom = true,
                  HUD = Hud
              },
              players,
              new Element[] { blocks },
              background
            );

            // zoom all the way out
            if (scene.Length > 10) for (int i=0; i<10; i++) World.Mousewheel(delta: -1f);

            // callbacks
            World.OnBeforeKeyPressed += CameraMovement;
            World.OnBeforeMousedown += World_OnBeforeMouseDown;
            World.OnBeforeMousemove += World_OnBeforeMousemove;
            World.OnAfterMousemove += World_OnAfterMousemove;
            World.OnAfterMouseup += World_OnAfterMouseup;
            Hud.OnSelectionChange += Hud_OnSelectionChange;
            Hud.OnSelectionChange += blocks.SetActivePheromone;
            Terrain.OnAddEgg += Terrain_OnAddEgg;

            // start the UI painting
            UI = new UIHookup(this, World);

        }

        #region private
        private UIHookup UI;
        private World World;
        private Camera Camera;
        private MouseButton MouseButton;
        private Controls Hud;
        private Terrain Terrain;
        private PheromoneType CurrentPheromone;

        private Ant CreateAnt(Terrain terrain, float x, float y, PheromoneType pheromone)
        {
            return new Ant(Terrain) { X = x, Y = y, Width = Terrain.BlockWidth / 4, Height = Terrain.BlockHeight / 2, Following = pheromone };
        }

        private void Terrain_OnAddEgg(float x, float y)
        {
            // the Terrain is requesting that an Egg (eg. Ant in egg form) be added at X,Y
            var egg = CreateAnt(Terrain, x, y, PheromoneType.None);
            egg.IsEgg = true;
            World.AddItem(egg);
        }

        private void Hud_OnSelectionChange(PheromoneType type)
        {
            CurrentPheromone = type;
        }

        private bool World_OnBeforeMouseDown(Element elem, MouseButton btn, float sx, float sy, float wx, float wy, float wz, ref char key)
        {
            // share with the Hud (check if 'handled')
            if (Hud.TryMouseDown(btn, sx, sy)) return true;

            // capture the button press
            MouseButton = btn;

            // call move to get 'click' semantics
            World_OnAfterMousemove(elem, sx, sy, wx, wy, wz);

            return true;
        }

        private void World_OnAfterMouseup(MouseButton btn)
        {
            // clear the active button
            MouseButton = MouseButton.None;
        }

        private bool World_OnBeforeMousemove()
        {
            // skip calculations if no button is pressed
            return (MouseButton != engine.Common.MouseButton.Left &&
                MouseButton != engine.Common.MouseButton.Right);
        }

        private void World_OnAfterMousemove(Element elem, float sx, float sy, float wx, float wy, float wz)
        {
            // exit early
            if (elem == null) return;
            if (elem is not Blocks) return;

            // translate into x,y within Terrain
            var tx = (wx - (elem.X - (elem.Width / 2))) - (Terrain.Width/2);
            var ty = (wy - (elem.Y - (elem.Height / 2))) - (Terrain.Height/2);

            // check if we have a control selected - only apply a pheromone if one is selected
            if (MouseButton == engine.Common.MouseButton.Left)
            {
                // add pheromone
                Terrain.TryApplyPheromone(tx, ty, CurrentPheromone);
            }
            else if (MouseButton == engine.Common.MouseButton.Right)
            {
                // clear pheromone
                Terrain.TryClearPheromone(tx, ty, CurrentPheromone);
            }
            else
            {
                return;
            }
            
            return;
        }

        // camera movement
        private bool CameraMovement(Player player, ref char key)
        {
            // zoom in/out
            if (key == '+' || key == '=')
            {
                World.Mousewheel(delta: 1f);
                return true;
            }
            else if (key == '-')
            {
                World.Mousewheel(delta: -1f);
                return true;
            }

            // view view point (need to teleport as the camera is technically touching the dirt)
            var speed = 10f;
            if (key == Constants.Left ||
                key == Constants.Left2 ||
                key == Constants.LeftArrow)
            {
                World.Teleport(Camera, Camera.X - speed, Camera.Y);
                return true;
            }
            else if (key == Constants.Right ||
                key == Constants.Right2 ||
                key == Constants.RightArrow)
            {
                World.Teleport(Camera, Camera.X + speed, Camera.Y);
                return true;
            }
            else if (key == Constants.Down ||
                key == Constants.Down2 ||
                key == Constants.DownArrow)
            {
                World.Teleport(Camera, Camera.X, Camera.Y + speed);
                return true;
            }
            else if (key == Constants.Up ||
                key == Constants.Up2 ||
                key == Constants.UpArrow)
            {
                World.Teleport(Camera, Camera.X, Camera.Y - speed);
                return true;
            }

            // capture left click
            if (key == Constants.LeftMouse)
            {
                // no sound on click
                // what is being hit is captured in the mouse down event
                return true;
            }

            return false;
        }
        #endregion

        #region protected
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            UI.ProcessCmdKey(keyData);
            return base.ProcessCmdKey(ref msg, keyData);
        } // ProcessCmdKey
        #endregion

    }
}
