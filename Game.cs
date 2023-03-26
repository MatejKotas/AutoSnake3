using System.Diagnostics;

namespace AutoSnake3
{
    static partial class Snake
    {
        public partial class Game
        {
            public Cell[,] Matrix;

            public bool gameOver = false;

            public int Tick;

            public int Length = StartingLength;
            public const int StartingLength = 2;

            public Cell Head; // Null at end of game

            public Cell Tail;

            public readonly int SizeX;
            public readonly int SizeY;

            public Cell Apple; // Null at end of game
            public Random AppleGenerator;

            public bool Automatic { get; private init; }

            public Game(int sizeX, int sizeY, bool automatic, int seed = -1)
            {
                SizeX = sizeX;
                SizeY = sizeY;
                Automatic = automatic;

                Matrix = new Cell[SizeX, SizeY];

                for (int x = 0; x < SizeX; x++)
                {
                    for (int y = 0; y < SizeY; y++)
                    {
                        Cell current = new(x, y, this);

                        if (x != 0)
                        {
                            current.Left = Matrix[x - 1, y];
                            current.Left.Right = current;
                        }

                        if (y != 0)
                        {
                            current.Down = Matrix[x, y - 1];
                            current.Down.Up = current;
                        }

                        Matrix[x, y] = current;
                    }
                }

                Head = Matrix[SizeX / 2, SizeY / 2];

                Head.SnakeTick = 1;
                Tail = Head.Up!;
                Tail.SnakeTick = 0;
                Tail.SnakeDirection = Direction.Down;

                if (seed != -1)
                    AppleGenerator = new(seed);
                else
                    AppleGenerator = new();

                NewApple();

                Debug.Assert(Apple != null);

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
            }

            public (int, bool) MakeMove()
            {
                if (!Automatic)
                    throw new InvalidOperationException("Game not initilized in automatic mode. Pass in a direction, or initilize game in automatic mode.");

                if (makeMove(Head.NextDirection) && !gameOver)
                {
                    Stopwatch elapsed = Stopwatch.StartNew();

                    CalculatePath();
                    return ((int)elapsed.ElapsedMilliseconds, true);
                }

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

            public void Print(bool snake, bool cycle)
            {
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
                        if (snake && Apple == Matrix[x, y])
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.Write("**");
                        }

                        else if ((snake && Matrix[x, y].Occupied()) || cycle)
                        {
                            Direction d = Direction.None;

                            if (snake && Matrix[x, y].Occupied())
                            {
                                d = Matrix[x, y].SnakeDirection;
                                Console.BackgroundColor = ConsoleColor.Green;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }
                            else
                            {
                                Console.BackgroundColor = ConsoleColor.Black;

                                if (cycle && Matrix[x, y].CycleDistance <= Apple.CycleDistance)
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
                          $"Ticks: { Tick + Length - StartingLength }\n" +
                          $"Length: { Length }\n" +
                          $"Apples Collected: { Length - StartingLength }");
            }
        }

        public enum Direction
        {
            None,
            Up,
            Right,
            Down,
            Left,
        }

        public static Direction ReverseDirection(Direction d)
        {
            return d switch
            {
                Direction.Up => Direction.Down,
                Direction.Right => Direction.Left,
                Direction.Down => Direction.Up,
                Direction.Left => Direction.Right,
                _ => throw new InvalidOperationException()
            };
        }
    }
}