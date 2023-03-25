namespace AutoSnake3
{
    static partial class Snake
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
#if false

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

            Game game = new(4, 4, mode == GameMode.Automatic);

            if (mode == GameMode.Automatic)
            {
                while (!game.gameOver)
                {
                    game.Print();
                    (int elapsed, _) = game.MakeMove();

                    if (elapsed < TickDelay)
                        Thread.Sleep(TickDelay - elapsed);
                }
            }

            else
            {
                Direction direction = Direction.Down;

                if (mode == GameMode.ManualSingle)
                    game.Print();

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
                    game.Print();

                    Thread.Sleep(TickDelay);
                }
            }
        }

#elif true

            Game? a = null;

            int seed = 0;

            while (true)
            {
                if (a == null || a.gameOver)
                    a = new(30, 30, true, 1);

                if (a.Head.NextDirection != ReverseDirection(a.Head.PreviousDirection))
                    Console.ReadLine();

                a.MakeMove();

                if (a.Head.NextDirection != ReverseDirection(a.Head.PreviousDirection))
                    a.Print(true, true);
            }
        }

#elif false

            Game a = new(4, 4, true, 4);

            for (int i = 0; i < 0; i++)
                a.MakeMove();

            a.Print(true, true);

            while (!a.gameOver)
            {
                a.MakeMove();
                a.Print(true, true);
            }
        }

#elif false

        Console.WriteLine("Start.");

        for (int j = 0; j < threads; j++)
        {
            Thread t = new(new ThreadStart(RunTests))
            {
                IsBackground = false
            };

            t.Start();
        }

        lock (threadLock)
            Console.WriteLine("Start complete.");
    }

    const int tests = 1000;
    const int threads = 8;
    const int sizeX = 6;
    const int sizeY = 6;

    static volatile bool running = true;
    static volatile object threadLock = new();
    static volatile int testCounter = 0;

    static void RunTests()
    {
        int seed = -1;

        while (running)
        {
            Monitor.Enter(threadLock);

            testCounter++;

            if (seed == -1)
                Console.WriteLine($"Playing game { testCounter }.");
            else
                Console.WriteLine($"Playing game { testCounter }. Game { seed } complete.");
            seed = testCounter;

            if (testCounter == tests)
            {
                running = false;
                Console.WriteLine("Finishing last simulations.");
            }

            Monitor.Exit(threadLock);

            Game game = new(sizeX, sizeY, true, seed);

            while (!game.gameOver)
                game.MakeMove(game.BestNextMove, true, iterations: 512);
        }
    }
#endif
    }
}