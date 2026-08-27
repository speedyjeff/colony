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

        public bool TryMouseDown(MouseButton btn, float x, float y)
        {
            // sanity check
            if (Buttons == null || Buttons.Count == 0) return false;

            // check if the click is outside of the bounds of the buttons
            if (x < Buttons[0].Left || x > Buttons[Buttons.Count - 1].Left + Buttons[Buttons.Count - 1].Width ||
                y < Buttons[0].Top  || y > Buttons[Buttons.Count - 1].Top + Buttons[Buttons.Count - 1].Height)
            {
                // do not check/change state
                return false;
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

            return true;
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
                var width = Math.Max(92f, g.Width * 0.075f);
                var height = Math.Max(34f, width * 0.34f);
                var padding = Math.Max(6f, height * 0.22f);

                // ensure the controls do not run off the screen
                while ((height * Buttons.Count) + (padding * Buttons.Count) > (g.Height - height)) padding--;

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
                var panelPadding = 12f;
                var panelLeft = Buttons[0].Left - panelPadding;
                var panelTop = Buttons[0].Top - panelPadding;
                var panelWidth = Buttons[0].Width + (panelPadding * 2);
                var panelBottom = Buttons[Buttons.Count - 1].Top + Buttons[Buttons.Count - 1].Height + panelPadding;
                g.Rectangle(Panel, panelLeft, panelTop, panelWidth, panelBottom - panelTop, fill: true, border: true, thickness: 1f);

                for (int i = 0; i<Buttons.Count; i++)
                {
                    g.Rectangle(Buttons[i].IsSelected ? Selected : Button,
                        Buttons[i].Left,
                        Buttons[i].Top,
                        Buttons[i].Width,
                        Buttons[i].Height,
                        fill: true,
                        border: true,
                        thickness: Buttons[i].IsSelected ? 3f : 1f);
                    g.Rectangle(Buttons[i].Color,
                        Buttons[i].Left,
                        Buttons[i].Top,
                        Buttons[i].Width * 0.08f,
                        Buttons[i].Height,
                        fill: true,
                        border: false);
                    g.Text(Text,
                        x: Buttons[i].Left + (Buttons[i].Width * 0.18f),
                        y: Buttons[i].Top + (Buttons[i].Height * 0.24f),
                        text: Buttons[i].Purpose,
                        fontsize: Math.Max(9f, Buttons[i].Height * 0.28f),
                        fontname: "Segoe UI");
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
        private static readonly RGBA Panel = new RGBA { R = 20, G = 22, B = 23, A = 235 };
        private static readonly RGBA Button = new RGBA { R = 38, G = 40, B = 40, A = 245 };
        private static readonly RGBA Selected = new RGBA { R = 68, G = 63, B = 54, A = 255 };
        private static readonly RGBA Text = new RGBA { R = 231, G = 226, B = 211, A = 255 };
        private float PreviousSurfaceWidth;
        private float PreviousSurfaceHeight;
        #endregion
    }
}
