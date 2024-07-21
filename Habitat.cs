using System;
using System.Windows.Forms;
using engine;
using engine.Winforms;
using engine.Common;
using engine.Common.Entities;

// architecture
//
// UI                                   | Game logic     | Entities
// -------------------------------------|----------------|----------------
//  World                               |  Terrain       |  Ant
//   - Handles UI screen drawing        |   - Game logic |  Blocks
//  Controls                            |                |  Queen
//   - Handles UI controls              |                |  Egg
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
            var background = new Background(width, height) { GroundColor = new RGBA { R = 100, G = 100, B = 100, A = 255 }, BasePace = 1f };

            // init
            MouseButton = engine.Common.MouseButton.None;
            CurrentPheromone = PheromoneType.None;
            Terrain = new Terrain(width: 1000, height: 1000, columns: 10, rows: 10);
            Terrain.Speed = background.BasePace * Constants.Speed;

            // add blocks
            var blocks = new Blocks(Terrain) { X = 0, Y = 0 };

            // add the camera and starting ants
            Camera = new Camera() { Name = "camera", X = 0, Y = 0 };
            var players = new Player[]
            {
                Camera,
                new Ant(Terrain) { Name = "", X = -10, Y = -1 * Terrain.BlockHeight, Width = Terrain.BlockWidth / 2, Height = Terrain.BlockHeight / 2, Following = PheromoneType.MoveDirt },
                new Ant(Terrain) { Name = "", X = -20, Y = -1 * Terrain.BlockHeight, Width = Terrain.BlockWidth / 2, Height = Terrain.BlockHeight / 2, Following = PheromoneType.MoveDirt },
                new Ant(Terrain) { Name = "", X = -30, Y = -1 * Terrain.BlockHeight, Width = Terrain.BlockWidth / 2, Height = Terrain.BlockHeight / 2, Following = PheromoneType.MoveDirt },
                new Ant(Terrain) { Name = "", X = 0, Y = -1 * Terrain.BlockHeight, Width = Terrain.BlockWidth / 2, Height = Terrain.BlockHeight / 2, Following = PheromoneType.MoveDirt }
            };

            // create the HUD
            Hud = new Controls();
            Hud.AddControl(PheromoneType.MoveDirt, new RGBA() { R = 255, G = 0, B = 0, A = 255 }, "dig");
            Hud.AddControl(PheromoneType.DropDirt, new RGBA() { R = 255, G = 0, B = 0, A = 100 }, "pile");

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

            // callbacks
            World.OnBeforeKeyPressed += CameraMovement;
            World.OnBeforeMousedown += World_OnBeforeMouseDown;
            World.OnBeforeMousemove += World_OnBeforeMousemove;
            World.OnAfterMousemove += World_OnAfterMousemove;
            World.OnAfterMouseup += World_OnAfterMouseup;
            Hud.OnSelectionChange += Hud_OnSelectionChange;
            Hud.OnSelectionChange += blocks.SetActivePheromone;

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

        private void Hud_OnSelectionChange(PheromoneType type)
        {
            CurrentPheromone = type;
        }

        private bool World_OnBeforeMouseDown(Element elem, MouseButton btn, float sx, float sy, float wx, float wy, float wz, ref char key)
        {
            // capture the button press
            MouseButton = btn;

            // share with the Hud
            Hud.MouseDown(btn, sx, sy);

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
            if (elem is not Blocks dirt) return;

            // debug
            //System.Diagnostics.Debug.WriteLine($"move: {elem.GetType()} {elem.Name} {sx},{sy} {wx},{wy},{wz} {wx - (elem.X - (elem.Width / 2))},{wy - (elem.Y - (elem.Height / 2))}");

            // check if we have a control selected - only apply a pheromone if one is selected
            if (MouseButton == engine.Common.MouseButton.Left)
            {
                // pass this along to Dirt to take action
                Terrain.ApplyPheromone(wx - (elem.X - (elem.Width / 2)), wy - (elem.Y - (elem.Height / 2)), CurrentPheromone);
            }
            else if (MouseButton == engine.Common.MouseButton.Right)
            {
                Terrain.ClearPheromone(wx - (elem.X - (elem.Width / 2)), wy - (elem.Y - (elem.Height / 2)), CurrentPheromone);
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
                key = Constants.Right;
                World.Teleport(Camera, Camera.X + speed, Camera.Y);
                return true;
            }
            else if (key == Constants.Right ||
                key == Constants.Right2 ||
                key == Constants.RightArrow)
            {
                key = Constants.Left;
                World.Teleport(Camera, Camera.X - speed, Camera.Y);
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
