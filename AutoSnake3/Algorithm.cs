namespace AutoSnake3
{
    public static partial class Snake
    {
        public partial class Game
        {
            internal int CycleDistanceIndexCounter = 0;
            internal int RevertOffset = 1;
            internal bool Reverted = false;

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
                DirectDistance = ShortestPathLength(Head, Tick);

                bool moveEnd = true;

                foreach (Cell.Neighbor source in Apple.StepSources)
                {
                    if (source.Direction == Apple.PreviousDirection)
                    {
                        moveEnd = false;
                        break;
                    }
                }

                Head.SetDistance(Apple);

                if (moveEnd)
                {
                    foreach (Cell.Neighbor source in Apple.StepSources)
                    {
                        if (source.Direction != Apple.NextDirection)
                        {
                            (bool succeeded, _, _) = TrySplice(source.Cell, ReverseDirection(source.Direction), DirectDistance, Tick, Area, 0);

                            if (succeeded)
                            {
                                Head.SetDistance(Apple);
                                break;
                            }
                        }
                    }
                }

                if (OptimizePath(Head, DirectDistance, Tick))
                {
                    AlgorithmDistance = Apple.CycleDistance;
                    return;
                }

                MoveList.AddLast(Head.NextDirection);

                Cell head = Head.Next;

                int tick = Tick + 1;
                int movesSinceLastStep = 1;
                int directDistanceToApple = DirectDistance - 1;

                while (head != Apple && head.Next != Apple && head.Next.Next != Apple)
                {
                    head.FutureSnakeTick = tick + Length - 1;

                    if (head.Step != movesSinceLastStep)
                    {
                        int updatedDirectDistanceToApple = ShortestPathLength(head, tick);

                        if (directDistanceToApple != updatedDirectDistanceToApple)
                        {
                            head.SetDistance(Apple);

                            if (OptimizePath(head, updatedDirectDistanceToApple, tick))
                                break;
                        }

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
                    AlgorithmDistance = head.SetDistance(Apple, startCount: tick - Tick) - 1; // For printing
            }

            // Returns if there is any further room for improvement
            bool OptimizePath(Cell head, int directDistanceToApple, int callTick)
            {
                bool runAgain;

                do
                {
                    Cell current = head;
                    runAgain = false;

                    while (Apple.CycleDistance - current.CycleDistance > current.DistanceTo(Apple))
                    {
                        foreach (Cell.Neighbor neighbor in current.Neighbors)
                        {
                            if (neighbor.Direction == current.NextDirection || neighbor.Direction == current.PreviousDirection || neighbor.Direction == ReverseDirection(current.NextDirection))
                                continue;

                            int distance = 1;
                            Cell? test = neighbor.Cell;

                            while (test != null && !test.Occupied(callTick))
                            {
                                if (test.CycleDistance <= Apple.CycleDistance)
                                {
                                    if (test.CycleDistance > current.CycleDistance)
                                    {
                                        if (TryBoxCut(head, current, neighbor.Direction, test, distance, callTick, directDistanceToApple)) // On seperate line for breakpoint
                                        {
                                            if (Apple.CycleDistance == directDistanceToApple)
                                                return true;

                                            runAgain = true;
                                        }
                                    }

                                    break;
                                }

                                test = test.Move(neighbor.Direction);
                                distance++;
                            }
                        }

                        current = current.Next;
                    }
                }
                while (runAgain);

                return Apple.CycleDistance == directDistanceToApple;
            }

            bool TryBoxCut(Cell head, Cell origin, Direction direction, Cell target, int distance, int tick, int directDistanceToApple)
            {
                head.SetDistance(origin);
                int apparentArea = target.SetDistance(Apple, false, origin.CycleDistance + distance);

                LinkedList<Splice> splices = new();

                int cycleDistance = origin.CycleDistance;

                do
                {
                    if (origin.NextDirection != direction)
                    {
                        (bool succeeded, Splice main, Splice? secondary) = TrySplice(origin, direction, directDistanceToApple, tick, apparentArea, target.CycleDistance);

                        if (succeeded)
                        {
                            splices.AddFirst(main);
                            splices.AddFirst((Splice)(secondary!));
                        }
                        else
                        {
                            foreach (var splice in splices)
                                splice.Reset();

                            head.DiscardLastCycleDistance();

                            return false;
                        }
                    }

                    origin = origin.Next;
                    origin.CycleDistance = ++cycleDistance;

                    distance--;
                }
                while (distance > 0);

                return true;
            }

            // Connects second.Previous to first.Next, first to second, and splices the two resulting cycles somewhere else
            // If splice fails, returns splice with null origin
            (bool succeeded, Splice main, Splice? secondary) TrySplice(Cell first, Direction direction, int directDistanceToApple, int tick, int apparentArea, int dontSpliceFrom) // dontSpliceTo is in essence Apple.CycleDistance
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
                    if (
                        (current.CycleDistance < dontSpliceFrom || current.CycleDistance >= Apple.CycleDistance)
                        && (!current.Occupied(tick + directDistanceToApple)))
                    {
                        foreach (Cell.Neighbor neighbor in current.Neighbors)
                        {
                            if (!neighbor.Cell.Seperated
                                && neighbor.Cell.CycleDistance > Apple.CycleDistance
                                && neighbor.Cell.Previous.DistanceTo(current.Next) == 1
                                && !neighbor.Cell.Occupied(tick + directDistanceToApple))
                            {
                                cycle.SetSeperated(false);

                                Splice secondary = new(current, neighbor.Direction);

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

            internal int ShortestPathLength(Cell start, int tick)
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

                    foreach (Cell.Neighbor n in current.Neighbors)
                    {
                        Cell neighbor = n.Cell;

                        int step;

                        if (neighbor.Occupied(tick + current.Step))
                        {
                            if (neighbor.FutureSnakeTick != -1)
                                step = neighbor.FutureSnakeTick - tick + 2;
                            else
                                step = neighbor.SnakeTick - tick + 2;
                        }
                        else
                            step = current.Step + 1;

                        if (neighbor.StepIndex != StepIndexCounter || neighbor.Step > step)
                        {
                            if (neighbor.StepIndex != StepIndexCounter)
                                neighbor.StepIndex = StepIndexCounter;

                            else
                                pending.Remove(neighbor);

                            if (neighbor == Apple)
                            {
                                // Make only shortest path(s) have step value

                                neighbor.StepSources.Clear();

                                foreach (Cell.Neighbor appleN in neighbor.Neighbors)
                                    if (appleN.Cell.Step == current.Step)
                                        neighbor.StepSources.Add(appleN);

                                ShortestPathTrace(neighbor, start);

                                StepIndexCounter++;

                                return step;
                            }

                            neighbor.Step = step;
                            neighbor.StepSources.Clear();
                            neighbor.StepSources.Add(neighbor.NeighborAt(ReverseDirection(n.Direction))!);

                            // Add to pending at correct position

                            LinkedListNode<Cell>? insertPoint = pending.First;

                            while (insertPoint != null)
                            {
                                if ((insertPoint.Value.StepLoss >= neighbor.StepLoss && insertPoint.Value.Step > neighbor.Step) || insertPoint.Value.StepLoss > neighbor.StepLoss)
                                {
                                    pending.AddBefore(insertPoint, neighbor);
                                    goto next;
                                }

                                insertPoint = insertPoint.Next;
                            }

                            pending.AddLast(neighbor);

                            next:;
                        }
                        else if (neighbor.Step == step)
                            neighbor.StepSources.Add(neighbor.NeighborAt(ReverseDirection(n.Direction))!);
                    }
                }

                throw new Exception("No path to apple found");
            }

            void ShortestPathTrace(Cell current, Cell start)
            {
                foreach (Cell.Neighbor s in current.StepSources)
                {
                    Cell source = s.Cell;

                    if (source.StepIndex == StepIndexCounter)
                    {
                        source.StepIndex++;

                        if (source != start)
                            ShortestPathTrace(source, start);
                    }
                }
            }
        }
    }
}