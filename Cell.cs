using System.Collections;

namespace AutoSnake3
{
    public static partial class Snake
    {
        public class Cell
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

            public Direction NextDirection
            {
                get => nextDirection;
                internal set
                {
                    nextDirection = value;
                    Next = Move(value)!;

                    Next.PreviousDirection = ReverseDirection(value);
                    Next.Previous = this;
                }
            }

            public Cell Next { get; private set; }

            public Direction PreviousDirection { get; private set; }
            public Cell Previous { get; private set; }

            #endregion

            #region CycleDistance

            int cycleDistance1;
            int cycleDistance2;

            int cycleDistanceIndex1 = -1;
            int cycleDistanceIndex2 = -1;

            internal int CycleDistance
            {
                get
                {
                    if (cycleDistanceIndex1 == parent.CycleDistanceIndexCounter)
                        return cycleDistance1;

                    else if (cycleDistanceIndex2 == parent.CycleDistanceIndexCounter)
                        return cycleDistance2;

                    else
                        return int.MaxValue;
                }

                set
                {
                    if (parent.CycleDistanceIndexCounter % 2 == 0)
                    {
                        cycleDistanceIndex1 = parent.CycleDistanceIndexCounter;
                        cycleDistance1 = value;
                    }

                    else
                    {
                        cycleDistanceIndex2 = parent.CycleDistanceIndexCounter;
                        cycleDistance2 = value;
                    }
                }
            }

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
            internal int SetDistance(Cell? stop, bool newIndex = true, int startCount = 0) // Returns ending count + 1
            {
                if (newIndex)
                {
                    if (!parent.Reverted)
                        parent.RevertOffset = 1;
                    else
                        parent.Reverted = false;

                    parent.CycleDistanceIndexCounter += parent.RevertOffset;
                }

                Cell current = this;

                if (stop == null)
                    stop = current;
                else
                    stop = stop.Next;

                do
                {
                    current.CycleDistance = startCount++;
                    current.Seperated = false;

                    current = current.Next;
                }
                while (current != stop);

                return startCount;
            }

            internal void DiscardLastCycleDistance()
            {
                parent.CycleDistanceIndexCounter -= parent.RevertOffset;
                parent.Reverted = true;
                parent.RevertOffset += 2;
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
                    Cell temp2 = cell.Previous;

                    cell.PreviousDirection = cell.NextDirection;
                    cell.Previous = cell.Next;

                    cell.nextDirection = temp;
                    cell.Next = temp2;

                    cell = cell.Next;
                }
                while (cell != this);
            }

            public Neighbor? NeighborAt(Direction direction) => Directions![(int)direction];

            public Cell? Move(Direction direction) => NeighborAt(direction)?.Cell;

            public int DistanceTo(Cell other) => Abs(X - other.X) + Abs(Y - other.Y);

            static int Abs(int value) => value >= 0 ? value : -value;
        }
    }
}