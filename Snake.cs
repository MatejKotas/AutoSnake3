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

                    if (mode == GameMode.Manual)
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

            Profiler profiler = new(Environment.ProcessorCount, 30, 30);
            profiler.RunTests(1, 1000, true, () => Console.Beep(880, 1000));
        }
#endif
    }
}