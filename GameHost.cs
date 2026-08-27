using System;
using engine;
using engine.Common;

namespace colony
{
    sealed class GameHost : IUserInteraction
    {
        public GameHost(Func<GameOptions, World> createWorld)
        {
            CreateWorld = createWorld;
            Current = new StartupScreen(StartGame);
        }

        public void InitializeGraphics(IGraphics surface, ISounds sounds)
        {
            Surface = surface;
            Sounds = sounds;
            Current.InitializeGraphics(surface, sounds);
        }

        public void Paint() => Current.Paint();
        public void Resize() => Current.Resize();
        public void KeyPress(char key) => Current.KeyPress(key);
        public void Mousewheel(float delta) => Current.Mousewheel(delta);
        public void Mousemove(float x, float y, float angle) => Current.Mousemove(x, y, angle);
        public void Mousedown(MouseButton btn, float x, float y) => Current.Mousedown(btn, x, y);
        public void Mouseup(MouseButton btn, float x, float y) => Current.Mouseup(btn, x, y);

        private readonly Func<GameOptions, World> CreateWorld;
        private IUserInteraction Current;
        private IGraphics? Surface;
        private ISounds? Sounds;
        private bool HasStarted;

        private void StartGame(GameOptions options)
        {
            if (HasStarted) return;
            if (Surface == null || Sounds == null) throw new InvalidOperationException("graphics are not initialized");

            HasStarted = true;
            var world = CreateWorld(options);
            world.InitializeGraphics(Surface, Sounds);
            world.KeyPress(Constants.Esc);
            Current = world;
        }
    }
}
