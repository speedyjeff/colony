namespace colony
{
    enum BoardType
    {
        EstablishedColony,
        CircularNest,
        FreshGround,
        OpenSwarm
    }

    sealed class GameOptions
    {
        public BoardType Board { get; init; } = BoardType.EstablishedColony;
        public bool ShowPheromoneTrails { get; init; } = true;
    }
}
