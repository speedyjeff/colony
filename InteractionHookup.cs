using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using engine.Common;
using engine.Winforms;

namespace colony
{
    sealed class InteractionHookup
    {
        public InteractionHookup(Control control, IUserInteraction interaction)
        {
            Control = control;
            Interaction = interaction;

            Control.SuspendLayout();
            try
            {
                Surface = new WritableGraphics(BufferedGraphicsManager.Current, Control.CreateGraphics(), Control.Height, Control.Width);
                Sounds = new Sounds(Control.Handle);
                Interaction.InitializeGraphics(Surface, Sounds);

                Control.Resize += OnResize;
                Control.Paint += OnPaint;
                Control.KeyPress += OnKeyPress;
                Control.MouseUp += OnMouseUp;
                Control.MouseDown += OnMouseDown;
                Control.MouseMove += OnMouseMove;
                Control.MouseWheel += OnMouseWheel;

                PaintTimer = new System.Windows.Forms.Timer { Interval = Constants.GlobalClock / 2 };
                PaintTimer.Tick += OnPaintTimer;
                PaintTimer.Start();

                MoveTimer = new System.Windows.Forms.Timer { Interval = Constants.GlobalClock / 2 };
                MoveTimer.Tick += OnMoveTimer;
            }
            finally
            {
                Control.ResumeLayout();
            }
        }

        public void ProcessCmdKey(Keys keyData)
        {
            if (keyData == Keys.Left) Interaction.KeyPress(Constants.LeftArrow);
            else if (keyData == Keys.Right) Interaction.KeyPress(Constants.RightArrow);
            else if (keyData == Keys.Up) Interaction.KeyPress(Constants.UpArrow);
            else if (keyData == Keys.Down) Interaction.KeyPress(Constants.DownArrow);
            else if (keyData == Keys.Escape) Interaction.KeyPress(Constants.Esc);
            else if (keyData == Keys.Enter) Interaction.KeyPress('\r');
        }

        private readonly Control Control;
        private readonly IUserInteraction Interaction;
        private readonly WritableGraphics Surface;
        private readonly Sounds Sounds;
        private readonly System.Windows.Forms.Timer PaintTimer;
        private readonly System.Windows.Forms.Timer MoveTimer;

        private void OnPaintTimer(object? sender, EventArgs e)
        {
            var duration = Stopwatch.StartNew();
            Interaction.Paint();
            Control.Refresh();
            duration.Stop();
            if (duration.ElapsedMilliseconds > (Constants.GlobalClock / 2) - 5)
            {
                Debug.WriteLine("**Paint Duration {0} ms", duration.ElapsedMilliseconds);
            }
        }

        private void OnMoveTimer(object? sender, EventArgs e)
        {
            Interaction.KeyPress(Constants.RightMouse);
        }

        private void OnPaint(object? sender, PaintEventArgs e)
        {
            Surface.RawRender(e.Graphics);
        }

        private void OnResize(object? sender, EventArgs e)
        {
            Surface.RawResize(Control.CreateGraphics(), Control.Height, Control.Width);
            Interaction.Resize();
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            Interaction.Mousewheel(e.Delta);
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            var angle = Collision.CalculateAngleFromPoint(Control.Width / 2f, Control.Height / 2f, e.X, e.Y);
            Interaction.Mousemove(e.X, e.Y, angle);
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) Interaction.KeyPress(Constants.LeftMouse);
            else if (e.Button == MouseButtons.Right) MoveTimer.Start();
            else if (e.Button == MouseButtons.Middle) Interaction.KeyPress(Constants.MiddleMouse);

            Interaction.Mousedown(ToEngineButton(e.Button), e.X, e.Y);
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) MoveTimer.Stop();
            Interaction.Mouseup(ToEngineButton(e.Button), e.X, e.Y);
        }

        private void OnKeyPress(object? sender, KeyPressEventArgs e)
        {
            Interaction.KeyPress(e.KeyChar);
        }

        private static engine.Common.MouseButton ToEngineButton(MouseButtons button)
        {
            if (button == MouseButtons.Left) return engine.Common.MouseButton.Left;
            if (button == MouseButtons.Middle) return engine.Common.MouseButton.Middle;
            return engine.Common.MouseButton.Right;
        }
    }
}
