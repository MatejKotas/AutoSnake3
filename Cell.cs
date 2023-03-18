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

            internal Cell[]? Neighbors;

            internal Direction NextDirection;
            internal Cell Next { get => Move(NextDirection)!; }

            internal int DistanceFromHead;

            internal Cell(int x, int y, Game parent)
            {
                X = x;
                Y = y;
                this.parent = parent;
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

            public bool IsNextTo(Cell other)
            {
                foreach (Cell c in Neighbors!)
                    if (c == other)
                        return true;

                return false;
            }

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

            class Enumerator : IEnumerator
            {
                Cell start;
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