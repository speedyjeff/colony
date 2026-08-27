using System;
using engine.Common;

namespace colony
{
    sealed class StartupScreen : IUserInteraction
    {
        public StartupScreen(Action<GameOptions> startGame)
        {
            StartGame = startGame;
        }

        public void InitializeGraphics(IGraphics surface, ISounds sounds)
        {
            Surface = surface;
        }

        public void Paint()
        {
            var g = Surface ?? throw new InvalidOperationException("graphics are not initialized");
            CalculateLayout(g.Width, g.Height);

            g.Clear(Background);

            var panelWidth = Math.Min(780f, g.Width - 48f);
            var panelHeight = Math.Min(650f, g.Height - 48f);
            var panelX = (g.Width - panelWidth) / 2f;
            var panelY = (g.Height - panelHeight) / 2f;
            g.Rectangle(Panel, panelX, panelY, panelWidth, panelHeight, fill: true, border: true, thickness: 1f);

            g.Text(PrimaryText, panelX + 42f, panelY + 30f, "COLONY", 32f, "Segoe UI Semibold");
            g.Text(SecondaryText, panelX + 44f, panelY + 86f, "The simulation is paused. Choose a habitat to begin.", 12f, "Segoe UI");
            g.Text(Accent, panelX + 44f, panelY + 130f, "HABITAT", 10f, "Segoe UI Semibold");

            for (var index = 0; index < BoardCards.Length; index++)
            {
                DrawBoardCard(g, index);
            }

            g.Rectangle(TrailEnabled ? Accent : Muted, TrailToggle.X, TrailToggle.Y, 22f, 22f, fill: true, border: true, thickness: 1f);
            if (TrailEnabled)
            {
                g.Line(Background, TrailToggle.X + 5f, TrailToggle.Y + 11f, TrailToggle.X + 10f, TrailToggle.Y + 16f, 2f);
                g.Line(Background, TrailToggle.X + 10f, TrailToggle.Y + 16f, TrailToggle.X + 18f, TrailToggle.Y + 6f, 2f);
            }
            g.Text(PrimaryText, TrailToggle.X + 34f, TrailToggle.Y - 1f, "Show pheromone trail overlays", 11f, "Segoe UI Semibold");
            g.Text(SecondaryText, TrailToggle.X + 34f, TrailToggle.Y + 24f, "Hidden trails still guide ants and remain editable.", 9f, "Segoe UI");

            g.Rectangle(Accent, StartButton.X, StartButton.Y, StartButton.Width, StartButton.Height, fill: true, border: false);
            g.Text(ButtonText, StartButton.X + 19f, StartButton.Y + 11f, "START COLONY", 11f, "Segoe UI Semibold");
        }

        public void Mousedown(MouseButton btn, float x, float y)
        {
            if (btn != MouseButton.Left) return;

            for (var index = 0; index < BoardCards.Length; index++)
            {
                if (BoardCards[index].Contains(x, y))
                {
                    SelectedBoard = (BoardType)index;
                    return;
                }
            }

            if (TrailToggle.Contains(x, y))
            {
                TrailEnabled = !TrailEnabled;
                return;
            }

            if (StartButton.Contains(x, y))
            {
                StartGame(new GameOptions
                {
                    Board = SelectedBoard,
                    ShowPheromoneTrails = TrailEnabled
                });
            }
        }

        public void KeyPress(char key)
        {
            if (key == '\r')
            {
                StartGame(new GameOptions
                {
                    Board = SelectedBoard,
                    ShowPheromoneTrails = TrailEnabled
                });
            }
        }

        public void Resize() { }
        public void Mousewheel(float delta) { }
        public void Mousemove(float x, float y, float angle) { }
        public void Mouseup(MouseButton btn, float x, float y) { }

        private readonly Action<GameOptions> StartGame;
        private readonly Bounds[] BoardCards = new Bounds[4];
        private IGraphics? Surface;
        private BoardType SelectedBoard = BoardType.EstablishedColony;
        private bool TrailEnabled = true;
        private Bounds TrailToggle;
        private Bounds StartButton;

        private static readonly string[] BoardNames =
        {
            "ESTABLISHED COLONY",
            "CIRCULAR NEST",
            "FRESH GROUND",
            "OPEN SWARM"
        };

        private static readonly string[] BoardDescriptions =
        {
            "Mature tunnels and chambers",
            "Radial underground habitat",
            "Build a colony from scratch",
            "Open field with 1,000 ants"
        };

        private static readonly RGBA Background = new RGBA { R = 18, G = 20, B = 22, A = 255 };
        private static readonly RGBA Panel = new RGBA { R = 27, G = 29, B = 30, A = 255 };
        private static readonly RGBA Card = new RGBA { R = 39, G = 41, B = 41, A = 255 };
        private static readonly RGBA SelectedCard = new RGBA { R = 65, G = 57, B = 47, A = 255 };
        private static readonly RGBA PrimaryText = new RGBA { R = 234, G = 229, B = 214, A = 255 };
        private static readonly RGBA SecondaryText = new RGBA { R = 164, G = 160, B = 149, A = 255 };
        private static readonly RGBA Accent = new RGBA { R = 196, G = 150, B = 92, A = 255 };
        private static readonly RGBA Muted = new RGBA { R = 70, G = 72, B = 70, A = 255 };
        private static readonly RGBA ButtonText = new RGBA { R = 24, G = 23, B = 20, A = 255 };

        private void CalculateLayout(float width, float height)
        {
            var panelWidth = Math.Min(780f, width - 48f);
            var panelHeight = Math.Min(650f, height - 48f);
            var panelX = (width - panelWidth) / 2f;
            var panelY = (height - panelHeight) / 2f;
            var cardGap = 14f;
            var cardWidth = (panelWidth - 102f) / 2f;
            var cardHeight = 105f;
            var cardsX = panelX + 44f;
            var cardsY = panelY + 160f;

            BoardCards[0] = new Bounds(cardsX, cardsY, cardWidth, cardHeight);
            BoardCards[1] = new Bounds(cardsX + cardWidth + cardGap, cardsY, cardWidth, cardHeight);
            BoardCards[2] = new Bounds(cardsX, cardsY + cardHeight + cardGap, cardWidth, cardHeight);
            BoardCards[3] = new Bounds(cardsX + cardWidth + cardGap, cardsY + cardHeight + cardGap, cardWidth, cardHeight);
            TrailToggle = new Bounds(cardsX, cardsY + ((cardHeight + cardGap) * 2f) + 18f, panelWidth - 88f, 54f);
            StartButton = new Bounds(panelX + panelWidth - 206f, panelY + panelHeight - 70f, 162f, 42f);
        }

        private void DrawBoardCard(IGraphics g, int index)
        {
            var bounds = BoardCards[index];
            var selected = SelectedBoard == (BoardType)index;
            g.Rectangle(selected ? SelectedCard : Card, bounds.X, bounds.Y, bounds.Width, bounds.Height, fill: true, border: true, thickness: selected ? 3f : 1f);
            g.Rectangle(BoardAccent(index), bounds.X, bounds.Y, 8f, bounds.Height, fill: true, border: false);
            g.Text(PrimaryText, bounds.X + 24f, bounds.Y + 22f, BoardNames[index], 11f, "Segoe UI Semibold");
            g.Text(SecondaryText, bounds.X + 24f, bounds.Y + 55f, BoardDescriptions[index], 9f, "Segoe UI");
        }

        private static RGBA BoardAccent(int index)
        {
            return index switch
            {
                0 => new RGBA { R = 155, G = 101, B = 65, A = 255 },
                1 => new RGBA { R = 139, G = 104, B = 151, A = 255 },
                2 => new RGBA { R = 174, G = 131, B = 81, A = 255 },
                3 => new RGBA { R = 105, G = 139, B = 91, A = 255 },
                _ => Accent
            };
        }

        private struct Bounds
        {
            public Bounds(float x, float y, float width, float height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public float X;
            public float Y;
            public float Width;
            public float Height;

            public bool Contains(float x, float y)
            {
                return x >= X && x <= X + Width && y >= Y && y <= Y + Height;
            }
        }
    }
}
