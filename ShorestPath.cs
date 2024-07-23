using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colony
{
    class ShortestPath
    {
        public ShortestPath(int rows, int columns)
        {
            // init
            NoUpdates = false;
            Rows = rows;
            Columns = columns;
            Cells = new Cell[rows][];
            for (int r = 0; r < Cells.Length; r++)
            {
                Cells[r] = new Cell[columns];
                for (int c = 0; c < columns; c++)
                {
                    Cells[r][c] = new Cell();
                }
            }
        }

        public int Rows { get; private set; }
        public int Columns { get; private set; }

        public bool NoUpdates { get; set; }

        public void SetTraversable(int row, int column, bool traversable)
        {
            if (row < 0 || column < 0 || row >= Cells.Length || column >= Cells[row].Length) throw new Exception("invalid row,column");
            Cells[row][column].IsTraversable = (traversable ? (byte)1 : (byte)0);

            // update all pheromones
            Update();
        }

        public void SetPheromone(int row, int column, PheromoneType type, DirectionType direction)
        {
            if (row < 0 || column < 0 || row >= Cells.Length || column >= Cells[row].Length) throw new Exception("invalid row,column");
            Cells[row][column].Directions[(int)type] = direction;

            // change just this pheromone
            Update(type);
        }

        public bool[] GetNextMove(int row, int col, PheromoneType pheromone)
        {
            // return a list of potential directions - PheromoneDirectionType as index
            var paths = new bool[]
            {
                false, // None
                false, // Up
                false, // Down
                false, // Left
                false  // Right
            };

            // if this is a 'destination' return the direction
            //  if not, choose a path that will get to that destination
            if (row < 0 || col < 0 || row >= Rows || col >= Columns) return paths;
            if (Cells[row][col].IsTraversable == 0) return paths;

            // return the viable paths
            if (Cells[row][col].Directions[(int)pheromone] != DirectionType.None)
            {
                // use the destination paths as a guide
                paths[(int)Cells[row][col].Directions[(int)pheromone]] = true;
            }
            else
            {
                // gather information about the neighbors
                var min = Int32.MaxValue;
                if (row - 1 >= 0) min = Math.Min(min, Cells[row - 1][col].Distances[(int)pheromone]);
                if (col + 1 < Columns) min = Math.Min(min, Cells[row][col + 1].Distances[(int)pheromone]);
                if (row + 1 < Rows) min = Math.Min(min, Cells[row + 1][col].Distances[(int)pheromone]);
                if (col - 1 >= 0) min = Math.Min(min, Cells[row][col - 1].Distances[(int)pheromone]);

                // exit early if there is no determined path
                if (min == Int32.MaxValue) return paths;

                // use the shortest path information
                if (row - 1 >= 0) paths[(int)DirectionType.Up] = (Cells[row - 1][col].Distances[(int)pheromone] == min);
                if (col + 1 < Columns) paths[(int)DirectionType.Right] = (Cells[row][col + 1].Distances[(int)pheromone] == min);
                if (row + 1 < Rows) paths[(int)DirectionType.Down] = (Cells[row + 1][col].Distances[(int)pheromone] == min);
                if (col - 1 >= 0) paths[(int)DirectionType.Left] = (Cells[row][col - 1].Distances[(int)pheromone] == min);
            }

            return paths;
        }

        public void Update()
        {
            if (Cells == null || Cells[0] == null || Cells[0].Length == 0 || Cells[0][0] == null) throw new Exception("must initialize");
            for (int pheromone = 1; pheromone < Cells[0][0].Directions.Length; pheromone++)
            {
                UpdateShortestPaths((PheromoneType)pheromone);
            }
        }

        public void Update(PheromoneType pheromone)
        {
            if (Cells == null || Cells[0] == null || Cells[0].Length == 0 || Cells[0][0] == null) throw new Exception("must initialize");
            UpdateShortestPaths(pheromone);
        }

        #region private
        class Cell
        {
            public byte IsTraversable;
            public DirectionType[] Directions;
            public int[] Distances;

            public Cell()
            {
                IsTraversable = 1;
                Distances = new int[]
                {
                Int32.MaxValue, // None = 0,
                Int32.MaxValue, // MoveDirt = 1,
                Int32.MaxValue, // MoveEgg = 3,
                Int32.MaxValue, // MoveFood = 4,
                Int32.MaxValue, // MoveDeadAnt = 5,
                Int32.MaxValue, // MoveQueen = 6,
                Int32.MaxValue, // DropDirt = 7,
                Int32.MaxValue, // DropEgg = 8,
                Int32.MaxValue, // DropFood = 9,
                Int32.MaxValue, // DropDeadAnt = 10
                };
                Directions = new DirectionType[]
                {
                DirectionType.None, // None = 0,
                DirectionType.None, // MoveDirt = 1,
                DirectionType.None, // MoveEgg = 3,
                DirectionType.None, // MoveFood = 4,
                DirectionType.None, // MoveDeadAnt = 5,
                DirectionType.None, // MoveQueen = 6,
                DirectionType.None, // DropDirt = 7,
                DirectionType.None, // DropEgg = 8,
                DirectionType.None, // DropFood = 9,
                DirectionType.None, // DropDeadAnt = 10
                };
            }
        }

        private Cell[][] Cells;

        private void UpdateShortestPaths(PheromoneType pheromone)
        {
            // check if updates are suspended
            if (NoUpdates) return;

            // init
            var visited = new HashSet<int>();
            var queue = new Queue<int>();

            // add the destination nodes to visit and reset the rest
            for (int row = 0; row < Cells.Length; row++)
            {
                for (int col = 0; col < Cells[row].Length; col++)
                {
                    // reset the distance
                    Cells[row][col].Distances[(int)pheromone] = Int32.MaxValue;

                    // check if a desired destination
                    if (Cells[row][col].Directions[(int)pheromone] != DirectionType.None)
                    {
                        // set to the minimal distance
                        Cells[row][col].Distances[(int)pheromone] = 0;
                        // get the index
                        RowColumnToIndex(row, col, out int index);
                        queue.Enqueue(index);
                    }
                }
            }

            // compute shortest path
            while (queue.Count > 0)
            {
                // check the next cell
                var index = queue.Dequeue();

                // check if visited
                if (visited.Contains(index)) continue;
                visited.Add(index);

                // get the row and column
                IndexToRowColumn(index, out int row, out int col);

                // consider the neighbors
                ChangeDistance(queue, row, col, row - 1, col, pheromone);
                ChangeDistance(queue, row, col, row + 1, col, pheromone);
                ChangeDistance(queue, row, col, row, col - 1, pheromone);
                ChangeDistance(queue, row, col, row, col + 1, pheromone);
            }
        }

        private bool ChangeDistance(Queue<int> queue, int row, int col, int neighborRow, int neighborCol, PheromoneType pheromone)
        {
            // only consider if within bounds
            if (neighborRow < 0 || neighborCol < 0 || neighborRow >= Rows || neighborCol >= Columns) return false;
            if (Cells[neighborRow][neighborCol].IsTraversable == 0) return false;

            var distance = Cells[row][col].Distances[(int)pheromone];

            // skip if there are no close destinations
            if (distance == Int32.MaxValue) return false;

            // compute the distances
            var prvDistance = Cells[neighborRow][neighborCol].Distances[(int)pheromone];
            var newDistance = distance + 1; // all edge distances are in 1 increments

            if (newDistance < prvDistance)
            {
                Cells[neighborRow][neighborCol].Distances[(int)pheromone] = newDistance;
                // get the index
                RowColumnToIndex(neighborRow, neighborCol, out int index);
                queue.Enqueue(index);
            }

            return true;
        }

        private void RowColumnToIndex(int row, int column, out int index)
        {
            index = (row * Rows) + column;
        }

        private void IndexToRowColumn(int index, out int row, out int column)
        {
            row = (index / Rows);
            column = (index % Rows);
        }
        #endregion
    }
}
