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
            }

            LinkedList<Direction> MoveList = new LinkedList<Direction>();

            void CalculatePath()
            {
                Cell head = Head;

                int previousDirectDistance = int.MinValue; // Bogus default value

                int moves = 0;
                int movesSinceLastStep = 0;

                StepIndexCounter++; // Clear Step

                while (head != Apple && head.Next != Apple && head.Next.Next != Apple)
                {
                    head.FutureSnakeTick = Tick + moves + Length - 1;

                    if (head.Step != movesSinceLastStep)
                    {
                        int directDistanceToApple = ShortestPathLength(head, Tick + moves);

                        if (previousDirectDistance - movesSinceLastStep != directDistanceToApple)
                            OptimizePath(head, directDistanceToApple, Tick + moves, false);
                        else
                            OptimizePath(head, directDistanceToApple, Tick + moves, true);

                        movesSinceLastStep = 0;
                        previousDirectDistance = directDistanceToApple;
                    }
                    else
                        OptimizePath(head, previousDirectDistance - movesSinceLastStep, Tick + moves, true);


                    MoveList.AddLast(head.NextDirection);

                    head = head.Next;
                    moves++;
                    movesSinceLastStep++;
                }

                head.SetDistance(Apple, moves); // For printing
            }

            bool OptimizePath(Cell head, int directDistanceToApple, int tick, bool onlyBoxCut)
            {
                head.SetDistance(null);

                Cell current = head;

                bool changed = false;

                while (current.CycleDistance <= Apple.CycleDistance && Apple.CycleDistance > directDistanceToApple)
                {
                    restart:

                    if (!onlyBoxCut && current.CycleDistance <= Apple.CycleDistance - 3)
                    {
                        foreach (Cell neighbor in current.Neighbors!)
                        {
                            if (neighbor.CycleDistance > current.CycleDistance
                                && neighbor != current.Next
                                && neighbor.CycleDistance <= Apple.CycleDistance)

                            {
                                (bool succeeded, _, _) = TrySplice(current, current.DirectionTowards(neighbor), directDistanceToApple);

                                if (succeeded)
                                {
                                    head.SetDistance(null);
                                    changed = true;

                                    // It seems restarting is the best way to guarantee the whole path gets streched out fully

                                    current = head;

                                    goto restart;
                                }
                            }
                        }
                    }

                    if (head != current && (head.X == current.X || head.Y == current.Y) && head.DistanceTo(current) > 1)
                    {
                        if (TryBoxCut(head, current, tick, directDistanceToApple))
                        {
                            head.SetDistance(null);
                            changed = true;

                            current = head;

                            goto restart;
                        }
                    }

                    current = current.Next;
                }

                return changed;
            }

            bool TryBoxCut(Cell start, Cell end, int tick, int directDistanceToApple)
            {
                Direction direction = start.DirectionTowards(end);

                if (start.NextDirection == direction || end.NextDirection == ReverseDirection(direction))
                    return false;

                int moves = 0;

                Cell current = start;

                while (true)
                {
                    current = current.Move(direction)!;
                    moves++;

                    if (current == end)
                        break;

                    if (current.Occupied(tick + moves - 1) || current.CycleDistance <= Apple.CycleDistance)
                        return false;
                }

                current = start;
                moves = 0;

                LinkedList<Splice> splices = new();

                do
                {
                    if (current.NextDirection != direction)
                    {
                        start.SetDistance(null);

                        (bool succeeded, Splice main, Splice? secondary) = TrySplice(current, direction, directDistanceToApple - moves);

                        if (succeeded)
                        {
                            splices.AddFirst(main);
                            splices.AddFirst((Splice)(secondary!));
                        }
                        else
                        {
                            foreach (var splice in splices)
                                splice.Reset();

                            start.SetDistance(null);

                            return false;
                        }
                    }

                    current = current.Move(direction)!;
                    moves++;
                }
                while (current != end);

                return true;
            }

            // Connects second.Previous to first.Next, first to second, and splices the two resulting cycles somewhere else
            // If splice fails, returns splice with null origin
            (bool succeeded, Splice main, Splice? secondary) TrySplice(Cell first, Direction direction, int directDistanceToApple)
            {
                Cell second = first.Move(direction)!;
                Cell cycle = second.Previous;

                Splice main = new(first, direction);

                if (main.Origin == null)
                    return (false, main, null);

                cycle.SetSeperated(true);

                Cell current = cycle.Next;

                do
                {
                    foreach (Cell neighbor in current.Neighbors!)
                    {
                        if (!neighbor.Seperated
                            && neighbor.CycleDistance > Apple.CycleDistance
                            && neighbor.Previous.DistanceTo(current.Next) == 1
                            && Area - Length + directDistanceToApple > neighbor.CycleDistance)
                        {
                            Splice secondary = new(current, current.DirectionTowards(neighbor));

                            //neighbor.Previous.NextDirection = neighbor.Previous.DirectionTowards(current.Next);
                            //current.NextDirection = current.DirectionTowards(neighbor);

                            return (true, main, secondary);
                        }
                    }

                    current = current.Next;
                }
                while (current != cycle);

                cycle.SetSeperated(false);
                main.Reset();

                return (false, main, null);
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
                        if (!neighbor.Occupied(tick + current.Step))
                        {
                            if (neighbor.StepIndex != StepIndexCounter)
                            {
                                if (neighbor == Apple)
                                {
                                    // Make only shortest path(s) have step value

                                    ShortestPathTrace(current, current.Step, start);

                                    StepIndexCounter++;
                                    current.StepIndex = StepIndexCounter;

                                    return current.Step + 1;
                                }

                                neighbor.StepIndex = StepIndexCounter;
                                neighbor.Step = current.Step + 1;
                                neighbor.StepSources[0] = current;
                                neighbor.StepSourcesIndex = 1;

                                // Add to pending at correct position

                                LinkedListNode<Cell>? insertPoint = pending.First;

                                while (insertPoint != null)
                                {
                                    if (insertPoint.Value.StepLoss > neighbor.StepLoss)
                                    {
                                        pending.AddBefore(insertPoint, neighbor);
                                        goto next;
                                    }

                                    insertPoint = insertPoint.Next;
                                }

                                pending.AddLast(neighbor);

                                next:;
                            }
                            else if (neighbor.Step == current.Step + 1)
                                neighbor.StepSources[neighbor.StepSourcesIndex++] = current;
                        }
                    }
                }

                throw new Exception("No path to apple found");
            }

            void ShortestPathTrace(Cell current, int step, Cell start)
            {
                for (int i = 0; i < current.StepSourcesIndex; i++)
                {
                    Cell source = current.StepSources[i];

                    if (source.StepIndex == StepIndexCounter)
                    {
                        source.StepIndex++;

                        if (source != start)
                            ShortestPathTrace(source, step - 1, start);
                    }
                }
            }
        }
    }
}