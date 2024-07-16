using engine.Common;
using engine.Common.Entities;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// warning: add is not thread safe, but it is not designed to be used in a multi-threaded environment

namespace colony
{
    class Controls : Menu
    {
        public Controls()
        {
            // set default controls
            Buttons = new List<ControlDetails>();
        }

        public Action<PheromoneType> OnSelectionChange { get; set; }

        public void AddControl(PheromoneType type, RGBA color, string purpose)
        {
            // add the control
            Buttons.Add(new ControlDetails { Type = type, Color = color, Purpose = purpose, IsSelected = false });

            // invalidate the dimensions
            PreviousSurfaceWidth = PreviousSurfaceHeight = 0;
        } 

        /*
        public bool TryGetSelectedId(out PheromoneType type)
        {
            for (int i = 0; i < Buttons.Count; i++)
            {
                if (Buttons[i].IsSelected)
                {
                    type = Buttons[i].Type;
                    return true;
                }
            }

            // did not find it
            type = PheromoneType.None;
            return false;
        }
        */

        public void MouseDown(MouseButton btn, float x, float y)
        {
            // sanity check
            if (Buttons == null || Buttons.Count == 0) return;

            // check if the click is outside of the bounds of the buttons
            if (x < Buttons[0].Left || x > Buttons[Buttons.Count - 1].Left + Buttons[Buttons.Count - 1].Width ||
                y < Buttons[0].Top  || y > Buttons[Buttons.Count - 1].Top + Buttons[Buttons.Count - 1].Height)
            {
                // do not check/change state
                return;
            }

            // change state on if the click is within the bounds of the buttons
            var notificationSent = false;
            for(int i=0; i<Buttons.Count; i++)
            {
                Buttons[i].IsSelected = (x >= Buttons[i].Left &&
                    x <= Buttons[i].Left + Buttons[i].Width &&
                    y >= Buttons[i].Top &&
                    y <= Buttons[i].Top + Buttons[i].Height);

                // notify that the selection has changed
                if (OnSelectionChange != null && Buttons[i].IsSelected)
                {
                    // notify the caller
                    OnSelectionChange(Buttons[i].Type);
                    notificationSent = true;
                }
            }

            // check if we need to clear the selection
            if (!notificationSent && OnSelectionChange != null)
            {
                OnSelectionChange(PheromoneType.None);
            }
        }

        public override void Draw(IGraphics g)
        {
            // realign the controls when resized
            if (g.Width != PreviousSurfaceWidth ||
                g.Height != PreviousSurfaceHeight)
            {
                // realign the controls
                var top = g.Height * 0.02f;
                var left = g.Width * 0.02f;
                var width = g.Width * 0.05f;
                var height = width;
                var padding = width / 2f;

                // set the control positions
                for (int i = 0; i < Buttons.Count; i++)
                {
                    Buttons[i].Top = top;
                    Buttons[i].Left = left;
                    Buttons[i].Width = width;
                    Buttons[i].Height = height;

                    // increment
                    top += height + padding;
                }

                // set previous
                PreviousSurfaceWidth = g.Width;
                PreviousSurfaceHeight = g.Height;
            }

            // draw the controls
            g.DisableTranslation();
            {
                for (int i = 0; i<Buttons.Count; i++)
                {
                    g.Rectangle(Buttons[i].Color,
                        Buttons[i].Left,
                        Buttons[i].Top,
                        Buttons[i].Width,
                        Buttons[i].Height,
                        fill: true,
                        border: Buttons[i].IsSelected,
                        thickness: 10);
                    g.Text(RGBA.Black, x: Buttons[i].Left + (Buttons[i].Width / 4), y: Buttons[i].Top + (Buttons[i].Height / 2), text: Buttons[i].Purpose, fontsize: 12f);
                }
            }
            g.EnableTranslation();
        }

        #region private
        class ControlDetails
        {
            public PheromoneType Type;
            public string Purpose;
            public float Top;
            public float Left;
            public float Width;
            public float Height;
            public RGBA Color;
            public bool IsSelected;
        }
        private List<ControlDetails> Buttons;
        private float PreviousSurfaceWidth;
        private float PreviousSurfaceHeight;
        #endregion
    }
}
