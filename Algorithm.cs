using System.Diagnostics;

namespace AutoSnake3
{
    public partial class Snake
    {
        public partial class Game
        {
            void InitilizeAlgorithm()
            {
                // Set neighbors

                foreach (Cell c in Matrix)
                {
                    List<Cell> neighbors = new();

                    if (c.Up != null)
                        neighbors.Add(c.Up);

                    if (c.Right != null)
                        neighbors.Add(c.Right);

                    if (c.Down != null)
                        neighbors.Add(c.Down);

                    if (c.Left != null)
                        neighbors.Add(c.Left);

                    c.Neighbors = neighbors.ToArray();
                }

                // Set starting hamiltonian cycle

                if (SizeX % 2 == 1 && SizeY % 2 == 1)
                    throw new Exception("SizeX or SizeY must be an even number for algorithm to work");

                else if (SizeX % 2 == 1)
                {
                    for (int y = 0; y < SizeY; y += 2)
                    {
                        for (int x = 1; x < SizeX; x++)
                        {
                            Matrix[x, y].NextDirection = Direction.Left;

                            if (x < SizeX - 1)
                                Matrix[x, y + 1].NextDirection = Direction.Right;
                        }

                        Matrix[SizeX - 1, y + 1].NextDirection = Direction.Down;

                        if (y > 0)
                            Matrix[1, y].NextDirection = Direction.Down;
                    }

                    Matrix[0, SizeY - 1].NextDirection = Direction.Right;
                    Matrix[1, 0].NextDirection = Direction.Left;

                    for (int y = 0; y < SizeY - 1; y++)
                        Matrix[0, y].NextDirection = Direction.Up;
                }
                else
                {
                    for (int x = 0; x < SizeX; x += 2)
                    {
                        for (int y = 1; y < SizeY; y++)
                        {
                            Matrix[x, y].NextDirection = Direction.Down;

                            if (y < SizeY - 1)
                                Matrix[x + 1, y].NextDirection = Direction.Up;
                        }

                        Matrix[x + 1, SizeY - 1].NextDirection = Direction.Left;

                        if (x > 0)
                            Matrix[x, 1].NextDirection = Direction.Left;
                    }

                    Matrix[SizeX - 1, 0].NextDirection = Direction.Up;
                    Matrix[0, 1].NextDirection = Direction.Down;

                    for (int x = 0; x < SizeX - 1; x++)
                        Matrix[x, 0].NextDirection = Direction.Right;
                }
            }

            void CalculatePath()
            {
                OptimizePath();
            }

            bool OptimizePath()
            {
                Head.SetDistance(Apple);

                int directDistanceToApple = Head.DistanceTo(Apple);

                Cell current = Head;

                bool changed = false;

                while (current.CycleDistance <= Apple.CycleDistance - 3 && Apple.CycleDistance > directDistanceToApple)
                {
                    restart:

                    foreach (Cell neighbor in current.Neighbors!)
                    {
                        if (neighbor.CycleDistance > current.CycleDistance && neighbor != current.Next && neighbor.CycleDistance <= Apple.CycleDistance && Splice(current, neighbor))
                        {
                            Head.SetDistance(Apple);
                            changed = true;

                            // It seems restarting is the best way to guarantee the whole path gets streched out fully

                            current = Head;

                            goto restart;
                        }
                    }

                    current = current.Next;
                }

                return changed;
            }

            // Connects b.Previous to a.Next, a to b, and joins the two cycles somewhere else
            bool Splice(Cell a, Cell b)
            {
                Debug.Assert(a != b);

                if (b.Previous.DistanceTo(a.Next) > 1)
                    return false;

                Direction splice2 = a.NextDirection; // In case the splice fails

                Cell cycle = b.Previous;

                Direction splice = a.DirectionTo(b);

                a.NextDirection = splice;
                cycle.NextDirection = ReverseDirection(splice);

                cycle.SetSeperated(true);

                Cell current = cycle;

                do
                {
                    foreach (Cell neighbor in current.Neighbors!)
                    {
                        if (!neighbor.Seperated && (neighbor.CycleDistance > Apple.CycleDistance || neighbor.CycleDistance == 0) && neighbor.Previous.DistanceTo(current.Next) == 1 && !neighbor.Occupied(Tick + Apple.CycleDistance - b.CycleDistance + a.CycleDistance))
                        {
                            Debug.Assert(neighbor.Previous.DistanceTo(current.Next) == 1);

                            neighbor.Previous.NextDirection = neighbor.Previous.DirectionTo(current.Next);
                            current.NextDirection = current.DirectionTo(neighbor);

                            cycle.SetSeperated(false);

                            return true;
                        }
                    }

                    current = current.Next;
                }
                while (current != cycle);

                cycle.SetSeperated(false);

                a.NextDirection = splice2;
                cycle.NextDirection = ReverseDirection(splice2);

                return false;
            }
        }
    }
}