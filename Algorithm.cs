namespace AutoSnake3
{
    public static partial class Snake
    {
        public partial class Game
        {
            internal int CycleDistanceIndexCounter = 0;

            void InitilizeAlgorithm()
            {
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

            readonly LinkedList<Direction> MoveList = new();

            void CalculatePath()
            {
                Cell head = Head;

                int directDistanceToApple = int.MinValue; // Bogus default value

                int tick = Tick;
                int movesSinceLastStep = 0;

                StepIndexCounter++; // Clear Step

                while (head != Apple && head.Next != Apple && head.Next.Next != Apple)
                {
                    head.FutureSnakeTick = tick + Length - 1;

                    if (head.Step != movesSinceLastStep)
                    {
                        int updatedDirectDistanceToApple = ShortestPathLength(head, tick);

                        if (directDistanceToApple != updatedDirectDistanceToApple && OptimizePath(head, updatedDirectDistanceToApple, tick))
                            break;

                        movesSinceLastStep = 0;
                        directDistanceToApple = updatedDirectDistanceToApple;
                    }

                    MoveList.AddLast(head.NextDirection);

                    head = head.Next;
                    tick++;
                    movesSinceLastStep++;
                    directDistanceToApple--;
                }

                if (head != Head)
                    Head.SetDistance(Apple); // For printing
            }

            // Returns if there is any further room for improvement
            bool OptimizePath(Cell head, int directDistanceToApple, int callTick)
            {
                head.SetDistance(Apple);

                restart:

                Cell current = head;

                while (Apple.CycleDistance - current.CycleDistance > current.DistanceTo(Apple))
                {
                    foreach (Cell neighbor in current.Neighbors!)
                    {
                        Direction direction = current.DirectionTowards(neighbor);

                        if (direction != current.NextDirection && direction != current.PreviousDirection)
                        {
                            int distance = 1;
                            Cell? test = neighbor;

                            while (test != null && !test.Occupied(callTick))
                            {
                                if (test.CycleDistance <= Apple.CycleDistance)
                                {
                                    if (test.CycleDistance > current.CycleDistance)
                                    {
                                        if (TryBoxCut(head, current, direction, test, distance, directDistanceToApple)) // On seperate line for breakpoint
                                        {
                                            if (Apple.CycleDistance == directDistanceToApple)
                                                return true;

                                            goto restart;
                                        }
                                    }

                                    break;
                                }

                                test = test.Move(direction);
                                distance++;
                            }
                        }
                    }

                    current = current.Next;
                }

                return false;
            }

            bool TryBoxCut(Cell head, Cell origin, Direction direction, Cell target, int distance, int directDistanceToApple)
            {
                head.SetDistance(origin);
                int apparentArea = target.SetDistance(head.Previous, false, origin.CycleDistance + distance);

                LinkedList<Splice> splices = new();

                int cycleDistance = origin.CycleDistance;

                do
                {
                    if (origin.NextDirection != direction)
                    {
                        (bool succeeded, Splice main, Splice? secondary) = TrySplice(origin, direction, directDistanceToApple, apparentArea, target.CycleDistance);

                        if (succeeded)
                        {
                            splices.AddFirst(main);
                            splices.AddFirst((Splice)(secondary!));
                        }
                        else
                        {
                            foreach (var splice in splices)
                                splice.Reset();

                            head.SetDistance(Apple);

                            return false;
                        }
                    }

                    origin = origin.Next;
                    origin.CycleDistanceIndex = CycleDistanceIndexCounter;
                    origin.CycleDistance = ++cycleDistance;

                    distance--;
                }
                while (distance > 0);

                head.SetDistance(Apple);
                return true;
            }

            // Connects second.Previous to first.Next, first to second, and splices the two resulting cycles somewhere else
            // If splice fails, returns splice with null origin
            (bool succeeded, Splice main, Splice? secondary) TrySplice(Cell first, Direction direction, int directDistanceToApple, int apparentArea, int dontSpliceFrom)
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
                    if (current.CycleDistance < dontSpliceFrom || current.CycleDistance >= Apple.CycleDistance)
                    {
                        foreach (Cell neighbor in current.Neighbors!)
                        {
                            if (!neighbor.Seperated
                                && neighbor.CycleDistance > Apple.CycleDistance
                                && neighbor.Previous.DistanceTo(current.Next) == 1
                                && apparentArea - Length + directDistanceToApple > neighbor.CycleDistance)
                            {
                                cycle.SetSeperated(false);

                                Splice secondary = new(current, current.DirectionTowards(neighbor));

                                return (true, main, secondary);
                            }
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