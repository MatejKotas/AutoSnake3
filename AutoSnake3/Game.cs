using System.Diagnostics;

namespace AutoSnake3
{
    public static partial class Snake
    {
        public partial class Game
        {
            public Cell[,] Matrix;

            public bool gameOver = false;

            internal int Tick;
            internal int TickAtLastApple { get; private set; }

            public int Moves
            {
                get => Tick + Length - StartingLength;
            }

            public int MovesSinceApple
            {
                get => Tick - TickAtLastApple;
            }

            public int DirectDistance { get; private set; }
            public int AlgorithmDistance { get; private set; }

            public int Length = StartingLength;
            public const int StartingLength = 2;

            public Cell Head; // Null at end of game

            public Cell Tail;

            public readonly int SizeX;
            public readonly int SizeY;

            public readonly int Area;

            public Cell Apple; // Null at end of game
            public Random AppleGenerator;

            public bool Automatic { get; private init; }

            public Game(int sizeX, int sizeY, bool automatic, int seed = -1)
            {
                SizeX = sizeX;
                SizeY = sizeY;
                Area = SizeX * SizeX;

                Automatic = automatic;

                Matrix = new Cell[SizeX, SizeY];

                for (int x = 0; x < SizeX; x++)
                    for (int y = 0; y < SizeY; y++)
                        Matrix[x, y] = new(x, y, this);

                // Set neighbors

                for (int x = 0; x < SizeX; x++)
                {
                    for (int y = 0; y < SizeY; y++)
                    {
                        List<Cell.Neighbor> neighbors = new();
                        Cell.Neighbor[] directions = new Cell.Neighbor[4];

                        if (y != sizeY - 1)
                        {
                            Cell.Neighbor n = new(Direction.Up, Matrix[x, y + 1]);

                            neighbors.Add(n);
                            directions[(int)Direction.Up] = n;
                        }

                        if (x != sizeX - 1)
                        {
                            Cell.Neighbor n = new(Direction.Right, Matrix[x + 1, y]);

                            neighbors.Add(n);
                            directions[(int)Direction.Right] = n;
                        }

                        if (y != 0)
                        {
                            Cell.Neighbor n = new(Direction.Down, Matrix[x, y - 1]);

                            neighbors.Add(n);
                            directions[(int)Direction.Down] = n;
                        }

                        if (x != 0)
                        {
                            Cell.Neighbor n = new(Direction.Left, Matrix[x - 1, y]);

                            neighbors.Add(n);
                            directions[(int)Direction.Left] = n;
                        }

                        Matrix[x, y].Neighbors = neighbors.ToArray();
                        Matrix[x, y].Directions = directions;
                    }
                }

                Head = Matrix[SizeX / 2, SizeY / 2];

                Head.SnakeTick = 1;
                Tail = Head.Move(Direction.Up)!;
                Tail.SnakeTick = 0;
                Tail.SnakeDirection = Direction.Down;

                if (seed != -1)
                    AppleGenerator = new(seed);
                else
                    AppleGenerator = new();

                NewApple();

                if (automatic)
                {
                    InitilizeAlgorithm();

                    CalculatePath();
                }
            }

            public void NewApple()
            {
                bool spaceIsAvailalbe = false;

                foreach (Cell c in Matrix)
                {
                    if (!c.Occupied())
                    {
                        spaceIsAvailalbe = true;
                        break;
                    }
                }

                if (spaceIsAvailalbe)
                {
                    while (true)
                    {
                        int x = AppleGenerator.Next(SizeX);
                        int y = AppleGenerator.Next(SizeY);

                        Cell result = Matrix[x, y];

                        if (!result.Occupied())
                        {
                            Apple = result;
                            break;
                        }
                    }
                }

                else
                {
                    Apple = null!;
                    gameOver = true;
                }

                TickAtLastApple = Tick;
            }

            public (int, bool) MakeMove()
            {
                if (!Automatic)
                    throw new InvalidOperationException("Game not initilized in automatic mode. Pass in a direction, or initilize game in automatic mode.");

                Direction move = NextMove;

                if (MoveList.Count > 0)
                    MoveList.RemoveFirst();

                Head.FutureSnakeTick = -1;

                if (makeMove(move) && !gameOver)
                {
                    Stopwatch elapsed = Stopwatch.StartNew();

                    CalculatePath();
                    return ((int)elapsed.ElapsedMilliseconds, true);
                }
                else if (gameOver)
                    return (0, true);

                return (0, false);
            }

            public bool MakeMove(Direction newDirection)
            {
                if (Automatic)
                    throw new InvalidOperationException("Game in automatic mode, can't pass in direciton parameter. Use parameterless version of MakeMove or initilize game in maual mode.");

                return makeMove(newDirection);
            }

            bool makeMove(Direction newDirection)
            {
                if (gameOver)
                    throw new InvalidOperationException("Game has already concluded.");

                Head!.SnakeDirection = newDirection;

                Head = Head.Move(newDirection)!;

                if (Head == null || Head.Occupied())
                {
                    gameOver = true;
                    return false;
                }

                Head.SnakeTick = Tick + Length;
                Head.SnakeDirection = Direction.None;

                if (Head == Apple)
                {
                    Length++;
                    NewApple();

                    return true;
                }

                else
                {
                    Tick++;
                    Tail = Tail.Move(Tail.SnakeDirection)!;
                }

                return false;
            }

            public Direction NextMove { get => MoveList.Count > 0 ? MoveList.First!.Value : Head.NextDirection; }

            public void Print(bool inline, bool snake = true, bool cycle = false) => DebugPrint(inline, snake: snake, cycle: cycle);

            internal void DebugPrint(bool inLine, bool snake = true, bool cycle = false, bool step = false, bool cycleDistance = false, bool seperated = false, int tick = -1)
            {
                int actualTick = tick == -1 ? Tick : tick;

                if (inLine)
                    Console.SetCursorPosition(0, 0);

                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.DarkGreen;

                Console.Write("    ");

                for (int i = 0; i < SizeX; i++)
                    Console.Write("  "); // Top border

                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine();

                for (int y = SizeY - 1; y >= 0; y--)
                {
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.Write("  "); // Left border

                    for (int x = 0; x < SizeX; x++)
                    {
                        bool occupied = tick == -1 ? Matrix[x, y].Occupied() : Matrix[x, y].Occupied(tick);

                        if (snake && Apple == Matrix[x, y])
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.Write("**");
                        }

                        else if ((snake && occupied) || cycle)
                        {
                            Direction d = Direction.None;

                            if (snake && occupied)
                            {
                                d = Matrix[x, y].SnakeDirection;

                                Console.BackgroundColor = ConsoleColor.Green;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }
                            else
                            {
                                Console.BackgroundColor = ConsoleColor.Black;

                                if (cycle && Matrix[x, y].Occupied(actualTick))
                                    Console.ForegroundColor = ConsoleColor.Green;

                                else if (cycle && Matrix[x, y].CycleDistance < Apple.CycleDistance && Matrix[x, y].CycleDistance > actualTick - TickAtLastApple)
                                    Console.ForegroundColor = ConsoleColor.Blue;

                                else
                                    Console.ForegroundColor = ConsoleColor.White;
                            }

                            if (cycle)
                                d = Matrix[x, y].NextDirection;

                            switch (d)
                            {
                                case Direction.Up:
                                    Console.Write("AA");
                                    break;

                                case Direction.Right:
                                    Console.Write(">>");
                                    break;

                                case Direction.Down:
                                    Console.Write("VV");
                                    break;

                                case Direction.Left:
                                    Console.Write("<<");
                                    break;

                                case Direction.None:
                                    Console.Write("OO");
                                    break;
                            }
                        }

                        else if (step && Matrix[x, y].Step != int.MaxValue)
                        {
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.White;

                            Console.Write((Matrix[x, y].Step.ToString() + "  ")[..2]);
                        }

                        else if (cycleDistance && Matrix[x, y].CycleDistance != int.MaxValue)
                        {
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.White;

                            Console.Write((Matrix[x, y].CycleDistance.ToString() + "  ")[..2]);
                        }

                        else if (seperated && Matrix[x, y].Seperated)
                        {
                            Console.BackgroundColor = ConsoleColor.White;
                            Console.Write("  ");
                        }

                        else
                        {
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.Write("  ");
                        }
                    }

                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.Write("  ");

                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine();
                }

                Console.BackgroundColor = ConsoleColor.DarkGreen;

                Console.Write("    ");

                for (int i = 0; i < SizeX; i++)
                    Console.Write("  "); // Bottom border

                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine($"\n\n" +
                          $"Moves: { Moves }\n" +
                          $"Length: { Length }\n" +
                          $"Apples Collected: { Length - StartingLength }");
            }
        }

        public enum Direction
        {
            None = -1,
            Up,
            Right,
            Down,
            Left,
        }

        public static Direction ReverseDirection(Direction d) =>
            d switch
            {
                Direction.Up => Direction.Down,
                Direction.Right => Direction.Left,
                Direction.Down => Direction.Up,
                Direction.Left => Direction.Right,
                _ => throw new InvalidOperationException()
            };
    }
}