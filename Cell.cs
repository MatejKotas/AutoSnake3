using System.Collections;

namespace AutoSnake3
{
    public static partial class Snake
    {
        public class Cell : IEnumerable // Enumerates the cells in the order of the hamiltonian cycle we are following
        {
            public class Neighbor
            {
                public readonly Direction Direction;
                public readonly Cell Cell;

                public Neighbor(Direction direction, Cell cell)
                {
                    Direction = direction;
                    Cell = cell;
                }
            }

            internal readonly Game parent;

            internal int SnakeTick = -1;
            internal Direction SnakeDirection;

            public readonly int X;
            public readonly int Y;

            internal Neighbor[]? Neighbors; // Elements guaranteed to be not null. Length varies.
            internal Neighbor[]? Directions { private get; set; } // Elements can be null. Use Direction as index. Length: 4

            #region Next

            Direction nextDirection;

            internal Direction NextDirection
            {
                get => nextDirection;
                set
                {
                    nextDirection = value;
                    Next.PreviousDirection = ReverseDirection(value);
                }
            }

            internal Cell Next { get => Move(nextDirection)!; }

            internal Direction PreviousDirection;
            internal Cell Previous { get => Move(PreviousDirection)!; }

            #endregion

            #region CycleDistance

            int cycleDistance;

            internal int CycleDistance
            {
                get => CycleDistanceIndex == parent.CycleDistanceIndexCounter ? cycleDistance : int.MaxValue;
                set => cycleDistance = value;
            }

            internal int CycleDistanceIndex = -1;

            #endregion

            internal bool Seperated = false;

            #region Step

            int step;

            internal int Step
            {
                get => StepIndex == parent.StepIndexCounter ? step : int.MaxValue;
                set
                {
                    step = value;
                    StepLoss = value + DistanceTo(parent.Apple);
                }
            }

            internal int StepIndex = -1;

            // Sum of Step and distance to apple. Used in Game.ShorestPath
            internal int StepLoss { get; private set; }

            internal List<Neighbor> StepSources = new(4); // All items are elements of Neighbors
            
            #endregion

            internal int FutureSnakeTick = -1;

            internal Cell(int x, int y, Game parent)
            {
                X = x;
                Y = y;
                this.parent = parent;
            }

            // Also sets seperated to false
            internal int SetDistance(Cell? stop, bool newIndex = true, int startCount = 0) // Returns ending count
            {
                if (newIndex)
                    parent.CycleDistanceIndexCounter++;

                Cell current = this;

                if (stop == null)
                    stop = current;
                else
                    stop = stop.Next;

                do
                {
                    current.cycleDistance = startCount++;
                    current.CycleDistanceIndex = parent.CycleDistanceIndexCounter;
                    current.Seperated = false;

                    current = current.Next;
                }
                while (current != stop);

                return startCount;
            }

            internal void SetSeperated(bool value)
            {
                Cell current = this;

                do
                {
                    current.Seperated = value;
                    current = current.Next;
                }
                while (current != this);
            }

            #region Occupied

            // Used for game
            public bool Occupied() => SnakeTick >= parent.Tick;

            // Used for algorithm
            public bool Occupied(int tick) => SnakeTick >= tick || FutureSnakeTick >= tick;

            public int OccupiedNeighbors()
            {
                int result = 4;

                foreach (Neighbor n in Neighbors!)
                    if (!n.Cell.Occupied())
                        result--;

                return result;
            }

            #endregion

            internal void ReverseCycle()
            {
                Cell cell = this;

                do
                {
                    Direction temp = cell.PreviousDirection;
                    cell.PreviousDirection = cell.NextDirection;
                    cell.nextDirection = temp;

                    cell = cell.Next;
                }
                while (cell != this);
            }

            public Neighbor? NeighborAt(Direction direction) => Directions![(int)direction];

            public Cell? Move(Direction direction) => NeighborAt(direction)?.Cell;

            public int DistanceTo(Cell other) => Abs(X - other.X) + Abs(Y - other.Y);

            static int Abs(int value) => value >= 0 ? value : -value;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IEnumerator GetEnumerator() => new Enumerator(this);

            class Enumerator : IEnumerator
            {
                readonly Cell start;
                Cell current;

                bool first = true;

                public Enumerator(Cell start)
                {
                    this.start = start;
                    current = start;
                }

                public object Current { get => current; }

                public bool MoveNext()
                {
                    if (!first)
                    {
                        current = current.Next;
                        return current != start;
                    }

                    first = false;
                    return true;
                }

                public void Reset()
                {
                    current = start;
                    first = false;
                }
            }
        }
    }
}