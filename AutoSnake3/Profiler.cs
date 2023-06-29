namespace AutoSnake3
{
    public static partial class Snake
    {
        public class Profiler
        {
            public class Result : IComparable
            {
                public int Seed { get; init; }
                public int Moves { get; init; }

                public Result(int seed, int moves)
                {
                    Seed = seed;
                    Moves = moves;
                }

                public int CompareTo(object? obj) => Moves.CompareTo((obj as Result)?.Moves);
            }

            public int Threads { get; init; }
            public int ThreadsRunning { get; private set; }

            public List<Result> Games { get; init; }
            object ThreadLock = new();

            int NextSeed;
            int EndSeed;

            int GamesComplete; // Per session variable

            readonly int SizeX;
            readonly int SizeY;

            readonly bool MinimumMode;
            int MinimumMoves = int.MaxValue;
            int MinimumSeed;

            public Profiler(int threads, int sizeX, int sizeY, bool minimumMode = false)
            {
                Threads = threads;
                SizeX = sizeX;
                SizeY = sizeY;

                Games = new();
                MinimumMode = minimumMode;
            }

            public delegate void ProfilerCallback();

            // startSeed and endSeed are inclusive
            public void RunTests(int startSeed, int endSeed, bool print, ProfilerCallback? callback = null)
            {
                NextSeed = startSeed;
                EndSeed = endSeed;

                for (; ThreadsRunning < Threads; ThreadsRunning++)
                {
                    Thread t = new(() => WorkerThread(print, callback)) { IsBackground = false, Name = $"Profiler Thread { ThreadsRunning }" };
                    t.Start();
                }

                if (print)
                    Console.WriteLine("All threads started.");
            }

            void WorkerThread(bool print, ProfilerCallback? callback)
            {
                Monitor.Enter(ThreadLock);

                while (NextSeed <= EndSeed && (!MinimumMode || MinimumMoves > SizeX * SizeY - Game.StartingLength))
                {
                    int seed = NextSeed++;

                    Monitor.Exit(ThreadLock);

                    Game game = new(SizeX, SizeY, true, seed);

                    if (MinimumMode)
                        while (!game.gameOver && game.Moves + (game.Area - game.Length) < MinimumMoves)
                            game.MakeMove();

                    else
                    {
                        try
                        {
                            while (!game.gameOver)
                                game.MakeMove();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }

                        if (game.Apple != null)
                        {
                            lock (ThreadLock)
                            {
                                Console.WriteLine($"Seed: { seed }");
                                game.Print(true, true, true);

                                throw new Exception("A game has failed. See console.");
                            }
                        }
                    }

                    Monitor.Enter(ThreadLock);

                    GamesComplete++;

                    if (MinimumMode)
                    {
                        if (game.gameOver && game.Moves < MinimumMoves)
                        {
                            MinimumMoves = game.Moves;
                            MinimumSeed = seed;

                            Console.WriteLine($"{ GamesComplete } / { EndSeed - NextSeed + GamesComplete + ThreadsRunning } games complete. Minimum moves so far: { MinimumMoves }");
                        }
                    }

                    else
                    {
                        Games.Add(new(seed, game.Moves));

                        if (print)
                            Console.WriteLine($"{ GamesComplete } / { EndSeed - NextSeed + GamesComplete + ThreadsRunning } games complete.");
                    }
                }

                ThreadsRunning--;

                if (print)
                {
                    if (ThreadsRunning == 0)
                    {
                        Console.WriteLine();

                        if (MinimumMode)
                            Console.WriteLine($"Game seed { MinimumSeed } with { MinimumMoves } moves");
                        else
                            PrintResults();
                    }
                }

                Monitor.Exit(ThreadLock);

                if (ThreadsRunning == 0 && callback != null)
                    callback();
            }

            public void PrintResults()
            {
                Games.Sort();

                float mean = 0;

                foreach (Result g in Games)
                    mean += g.Moves;

                mean /= Games.Count;

                float standardDeviation = 0;

                foreach (Result g in Games)
                    standardDeviation += MathF.Pow(mean - g.Moves, 2);

                standardDeviation = MathF.Sqrt(standardDeviation / Games.Count);

                float median;

                if (Games.Count % 2 == 0)
                    median = ((float)(Games[Games.Count / 2].Moves + Games[Games.Count / 2 - 1].Moves)) / 2;
                else
                    median = Games[Games.Count / 2].Moves;

                Tuple<string, float>[] data = {
                    new("Mean", mean),
                    new("Standard Deviation", standardDeviation),
                    new("Minimum", Games[0].Moves),
                    new("Median", median),
                    new("Maximum", Games[Games.Count - 1].Moves)
                };

                string headers = "|";
                string seperators = "|";
                string values = "|";

                foreach (Tuple<string, float> t in data)
                {
                    string value = MathF.Round(t.Item2, 2).ToString();
                    string name = t.Item1;
                    string seperator = "";

                    if (name.Length > value.Length)
                        while (name.Length > value.Length)
                            value += " ";

                    else
                        while (value.Length > name.Length)
                            name += " ";

                    while (name.Length > seperator.Length)
                        seperator += "-";

                    headers += " " + name + " |";
                    seperators += " " + seperator + " |";
                    values += " " + value + " |";
                }

                Console.WriteLine(headers);
                Console.WriteLine(seperators);
                Console.WriteLine(values);
                Console.WriteLine();
                Console.WriteLine($"Minimum moves game seed: { Games[0].Seed }");

                if (Games.Count % 2 == 0)
                    Console.WriteLine($"Median moves game seeds: { Games[Games.Count / 2].Seed } and { Games[Games.Count / 2 - 1].Seed }");

                else
                    Console.WriteLine($"Median moves game seed: { Games[Games.Count / 2].Seed }");

                Console.WriteLine($"Maximum moves game seed: { Games[Games.Count - 1].Seed }");
            }
        }
    }
}