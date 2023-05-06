namespace AutoSnake3
{
    public static partial class Snake
    {
        const int TickDelay = 400;

        public enum GameMode
        {
            Unset,
            Manual = 1,
            ManualSingle = 2,
            Automatic = 3
        }

        static void Main(string[] args)
        {
#if true

            GameMode mode = GameMode.Unset;

            while (mode == GameMode.Unset)
            {
                Console.WriteLine("1. Manual");
                Console.WriteLine("2. Manual single-step");
                Console.WriteLine("3. Automatic");

                mode = Console.ReadLine() switch
                {
                    "1" => GameMode.Manual,
                    "2" => GameMode.ManualSingle,
                    "3" => GameMode.Automatic,
                    _ => GameMode.Unset
                };
            }

            Game game = new(12, 12, mode == GameMode.Automatic);

            if (mode == GameMode.Automatic)
            {
                while (!game.gameOver)
                {
                    game.Print(true, true);
                    (int elapsed, _) = game.MakeMove();

                    if (elapsed < TickDelay)
                        Thread.Sleep(TickDelay - elapsed);
                }

                game.Print(true, true);
            }

            else
            {
                Direction direction = Direction.Down;

                if (mode == GameMode.ManualSingle)
                    game.Print(true, false);

                while (!game.gameOver)
                {
                    while (mode == GameMode.ManualSingle && !Console.KeyAvailable) { Thread.Sleep(1); }

                    if (Console.KeyAvailable)
                    {
                        direction = Console.ReadKey(true).Key switch
                        {
                            ConsoleKey.UpArrow => Direction.Up,
                            ConsoleKey.RightArrow => Direction.Right,
                            ConsoleKey.DownArrow => Direction.Down,
                            ConsoleKey.LeftArrow => Direction.Left,
                            _ => direction
                        };
                    }

                    game.MakeMove(direction);
                    game.Print(true, false);

                    Thread.Sleep(TickDelay);
                }
            }
        }

#elif false

            Game? a = null;

            int seed = 0;
            bool newApple = false;

            while (true)
            {
                if (a == null || a.gameOver)
                    a = new(12, 12, true, seed++);

                Direction lastMove = a.NextMove;

                (_, newApple) = a.MakeMove();

                if (newApple || lastMove != a.NextMove)
                {
                    a.Print(true, true);

                    Console.ReadLine();
                }
            }
        }

#elif false

            Game a = new(12, 12, true, 0);

            for (int i = 0; i < 0 && !a.gameOver; i++)
                a.MakeMove();

            a.Print(true, true);

            while (!a.gameOver)
            {
                a.MakeMove();

                a.DebugPrint(true, true, true);
            }
        }

#elif false

            Console.WriteLine("Start.");

            for (int j = 0; j < threads && j < tests; j++)
            {
                Thread t = new(new ThreadStart(RunTests))
                {
                    IsBackground = false
                };

                t.Start();

                threadsRunning++;
            }

            lock (threadLock)
                Console.WriteLine("Start complete.");
        }

        const int tests = 1000;
        const int threads = 8;
        const int sizeX = 30;
        const int sizeY = 30;

        static volatile bool running = true;
        static volatile int threadsRunning;
        static volatile object threadLock = new();
        static volatile int testCounter = 0;
        
        static int totalMoves = 0;

        static int minMoves = int.MaxValue;
        static int minMovesSeed;

        static int maxMoves = 0;
        static int maxMovesSeed;

        static void RunTests()
        {
            int seed = -1;
            int moves = 0;

            while (running)
            {
                Monitor.Enter(threadLock);

                testCounter++;

                if (seed == -1)
                    Console.WriteLine($"Playing game { testCounter }.");
                else
                {
                    Console.WriteLine($"Playing game { testCounter }. Game { seed } complete.");
                    totalMoves += moves;
                }

                seed = testCounter;

                if (testCounter == tests)
                {
                    running = false;
                    Console.WriteLine("Finishing last games.");
                }

                Monitor.Exit(threadLock);

                Game game = new(sizeX, sizeY, true, seed);

                while (!game.gameOver)
                    game.MakeMove();

                if (game.Apple != null)
                {
                    lock (threadLock)
                    {
                        running = false;

                        Console.WriteLine($"Seed: { seed }");
                        game.Print(true, true);

                        throw new Exception();
                    }
                }

                moves = game.Moves;

                if (minMoves > game.Moves)
                {
                    minMoves = game.Moves;
                    minMovesSeed = seed;
                }

                if (maxMoves < game.Moves)
                {
                    maxMoves = game.Moves;
                    maxMovesSeed = seed;
                }
            }

            lock (threadLock)
            {
                threadsRunning--;

                Console.WriteLine($"Game { seed } complete. Remaining games: { threadsRunning }");
                totalMoves += moves;

                if (threadsRunning == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine($"All games complete.");
                    Console.WriteLine($"Avg moves per game: { totalMoves / tests }");
                    Console.WriteLine($"Max moves in a game: { maxMoves }. Seed: { maxMovesSeed }");
                    Console.WriteLine($"Min moves in a game: { minMoves }. Seed: { minMovesSeed }");
                }
            }
        }
#endif
    }
}