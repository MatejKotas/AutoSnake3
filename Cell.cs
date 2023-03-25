using System.Collections;
using System.Runtime.Serialization;

namespace AutoSnake3
{
    static partial class Snake
    {
        public class Cell : IEnumerable // Enumerates the cells in the order of the hamiltonian cycle we are following
        {
            internal readonly Game parent;

            internal int SnakeTick = -1;
            internal Direction SnakeDirection;

            public readonly int X;
            public readonly int Y;

            public Cell? Up { get; internal set; }
            public Cell? Right { get; internal set; }
            public Cell? Down { get; internal set; }
            public Cell? Left { get; internal set; }

            internal Cell[]? Neighbors; // Only set in automatic mode

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

            int cycleDistance;

            internal int CycleDistance
            {
                get => CycleDistanceIndex == CycleDistanceIndexCounter ? cycleDistance : int.MaxValue;
            }

            internal int CycleDistanceIndex = -1;
            internal bool Seperated = false;

            static int CycleDistanceIndexCounter = 0;

            internal Cell(int x, int y, Game parent)
            {
                X = x;
                Y = y;
                this.parent = parent;
            }

            // Also sets seperated to false
            internal int SetDistance(Cell stop, int startCount, bool @continue) // Returns cycle length
            {
                if (!@continue)
                    CycleDistanceIndexCounter++;

                int count = startCount;
                Cell current = this;

                stop = stop.Next;

                do
                {
                    current.cycleDistance = count++;
                    current.CycleDistanceIndex = CycleDistanceIndexCounter;
                    current.Seperated = false;

                    current = current.Next;
                }
                while (current != stop);

                return startCount - count;
            }

            internal void SetSeperated(bool value) // Returns cycle length
            {
                Cell current = this;

                do
                {
                    current.Seperated = value;
                    current = current.Next;
                }
                while (current != this);
            }

            public bool Occupied() => SnakeTick >= parent.Tick;

            public Cell? Move(Direction direction)
            {
                return direction switch
                {
                    Direction.Up => Up,
                    Direction.Right => Right,
                    Direction.Down => Down,
                    Direction.Left => Left,
                    _ => throw new InvalidOperationException()
                };
            }

            internal Direction DirectionTo(Cell target)
            {
                if (target == Up)
                    return Direction.Up;

                else if (target == Right)
                    return Direction.Right;

                else if (target == Down)
                    return Direction.Down;

                else if (target == Left)
                    return Direction.Left;

                return Direction.None;
            }

            public int DistanceTo(Cell other) => Abs(X - other.X) + Abs(Y - other.Y);

            static int Abs(int value) => value >= 0 ? value : -value;

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

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