namespace AutoSnake3
{
    public static partial class Snake
    {
        const int TickDelay = 400;

        public enum GameMode
        {
            Unset,
            Manual,
            ManualSingle,
            ManualSingleAssisted,
            Automatic
        }

        static void Main(string[] args)
        {
#if true

            GameMode mode = GameMode.Unset;

            while (mode == GameMode.Unset)
            {
                Console.WriteLine("1. Manual");
                Console.WriteLine("2. Manual single-step");
                Console.WriteLine("3. Manual single-step with assistance");
                Console.WriteLine("4. Automatic");

                mode = Console.ReadLine() switch
                {
                    "1" => GameMode.Manual,
                    "2" => GameMode.ManualSingle,
                    "3" => GameMode.ManualSingleAssisted,
                    "4" => GameMode.Automatic,
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

                if (mode == GameMode.ManualSingle || mode == GameMode.ManualSingleAssisted)
                    game.Print(true, false);

                while (!game.gameOver)
                {
                    if (mode == GameMode.ManualSingle || mode == GameMode.ManualSingleAssisted)
                        while (!Console.KeyAvailable) { Thread.Sleep(1); }

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

                    if (mode == GameMode.ManualSingleAssisted && game.Head.OccupiedNeighbors() < 4 && (game.Head.Move(direction) == null || game.Head.Move(direction)!.Occupied()))
                        continue;

                    game.MakeMove(direction);

                    if (mode == GameMode.ManualSingleAssisted && game.Head.OccupiedNeighbors() == 3)
                    {
                        while (game.Head.OccupiedNeighbors() == 3)
                        {
                            foreach (Cell.Neighbor neighbor in game.Head.Neighbors)
                            {
                                if (!neighbor.Cell.Occupied())
                                {
                                    game.MakeMove(neighbor.Direction);
                                    break;
                                }
                            }
                        }
                    }

                    game.Print(true, false);

                    if (mode == GameMode.Manual)
                        Thread.Sleep(TickDelay);
                }
            }
        }

#elif false

            const bool onlyLongPaths = true;

            Game? a = null;

            int seed = 0;
            bool newApple = false;

            while (true)
            {
                if (a == null || a.gameOver)
                    a = new(12, 12, true, seed++);

                Direction lastMove;

                if (a.DirectDistance == a.AlgorithmDistance && onlyLongPaths)
                {
                    do
                    {
                        lastMove = a.NextMove;
                        (_, newApple) = a.MakeMove();
                    }
                    while (!newApple);
                }

                else
                {
                    a.Print(true, true);

                    Console.ReadLine();

                    do
                    {
                        lastMove = a.NextMove;
                        (_, newApple) = a.MakeMove();
                    }
                    while (lastMove == a.NextMove && !newApple);
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