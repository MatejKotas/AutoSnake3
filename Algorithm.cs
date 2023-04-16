using System.Diagnostics;

namespace AutoSnake3
{
    public partial class Snake
    {
        public partial class Game
        {
            internal int CycleDistanceIndexCounter = 0;

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

                if (Tail.SnakeDirection != Tail.NextDirection)
                    Head.ReverseCycle();

                Debug.Assert(Tail.SnakeDirection == Tail.NextDirection);
            }

            void CalculatePath()
            {
                Cell head = Head;

                int previousDirectDistance = int.MinValue; // Bogus default value
                int moves = 0;

                while (head != Apple && head.Next != Apple && head.Next.Next != Apple)
                {
                    int directDistanceToApple = ShortestPathLength(head, Tick + moves);

                    if (previousDirectDistance - 1 != directDistanceToApple)
                        OptimizePath(head, directDistanceToApple);

                    head.FutureSnakeTick = Tick + moves;

                    previousDirectDistance = directDistanceToApple;
                    head = head.Next;
                    moves++;
                }

                head = Head;

                while (head.Next != Apple && head.Next.Next != Apple)
                {
                    head.FutureSnakeTick = -1;
                    head = head.Next;
                }

                Head.SetDistance(Apple);
            }

            bool OptimizePath(Cell head, int directDistanceToApple)
            {
                head.SetDistance(null);

                Cell current = head;

                bool changed = false;

                while (current.CycleDistance <= Apple.CycleDistance - 3 && Apple.CycleDistance > directDistanceToApple)
                {
                    restart:

                    foreach (Cell neighbor in current.Neighbors!)
                    {
                        if (neighbor.CycleDistance > current.CycleDistance
                            && neighbor != current.Next
                            && neighbor.CycleDistance <= Apple.CycleDistance
                            && Splice(current, neighbor, directDistanceToApple))
                        {
                            head.SetDistance(null);
                            changed = true;

                            // It seems restarting is the best way to guarantee the whole path gets streched out fully

                            current = head;

                            goto restart;
                        }
                    }

                    current = current.Next;
                }

                return changed;
            }

            // Connects second.Previous to first.Next, first to second, and splices the two resulting cycles somewhere else
            bool Splice(Cell first, Cell second, int directDistanceToApple)
            {
                Debug.Assert(first != second);

                if (second.Previous.DistanceTo(first.Next) > 1)
                    return false;

                Direction splice2 = first.NextDirection; // In case the splice fails

                Cell cycle = second.Previous;

                Direction splice = first.DirectionTo(second);

                first.NextDirection = splice;
                cycle.NextDirection = ReverseDirection(splice);

                //Print(true, true);

                cycle.SetSeperated(true);

                Cell current = cycle;

                do
                {
                    foreach (Cell neighbor in current.Neighbors!)
                    {
                        if (!neighbor.Seperated
                             && neighbor.CycleDistance > Apple.CycleDistance
                            && neighbor.Previous.DistanceTo(current.Next) == 1
                            && Area - Length + directDistanceToApple > neighbor.CycleDistance)
                        {
                            Debug.Assert(neighbor.Previous.DistanceTo(current.Next) == 1);

                            neighbor.Previous.NextDirection = neighbor.Previous.DirectionTo(current.Next);
                            current.NextDirection = current.DirectionTo(neighbor);

                            return true;
                        }
                    }

                    current = current.Next;
                }
                while (current != cycle);

                cycle.SetSeperated(false);

                first.NextDirection = splice2;
                cycle.NextDirection = ReverseDirection(splice2);

                return false;
            }

            internal int StepIndexCounter = 0;

            int ShortestPathLength(Cell start, int tick)
            {
                LinkedList<Cell> pending = new();
                pending.AddLast(start);

                StepIndexCounter++;

                start.Step = 0;
                start.StepIndex = StepIndexCounter;

                while (pending.Count > 0)
                {
                    Cell current = pending.First!.Value;
                    pending.RemoveFirst();

                    foreach (Cell neighbor in current.Neighbors!)
                    {
                        if (!neighbor.Occupied(tick + current.Step) && neighbor.StepIndex != StepIndexCounter)
                        {
                            if (neighbor == Apple)
                                return current.Step + 1;

                            neighbor.StepIndex = StepIndexCounter;
                            neighbor.Step = current.Step + 1;

                            // Add at correct position

                            int loss = neighbor.Step + neighbor.DistanceTo(Apple);

                            LinkedListNode<Cell>? insertPoint = pending.First;

                            while (insertPoint != null)
                            {
                                if (insertPoint.Value.Step + insertPoint.Value.DistanceTo(Apple) > loss)
                                {
                                    pending.AddBefore(insertPoint, neighbor);
                                    goto next;
                                }

                                insertPoint = insertPoint.Next;
                            }

                            pending.AddLast(neighbor);

                            next:;
                        }
                    }
                }

                throw new Exception("No path to apple found");
            }
        }
    }
}