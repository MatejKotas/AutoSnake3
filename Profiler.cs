namespace AutoSnake3
{
    public static partial class Snake
    {
        public class Profiler
        {
            public int Threads { get; init; }
            public int ThreadsRunning { get; private set; }

            public List<int> Moves { get; init; }
            object ThreadLock = new();

            int NextSeed;
            int EndSeed;

            int GamesComplete; // Per session variable

            readonly int SizeX;
            readonly int SizeY;

            public Profiler(int threads, int sizeX, int sizeY)
            {
                Threads = threads;
                SizeX = sizeX;
                SizeY = sizeY;

                Moves = new();
            }

            public delegate void ProfilerCallback();

            // startSeed and endSeed are inclusive
            public void RunTests(int startSeed, int endSeed, bool print, ProfilerCallback? callback = null)
            {
                NextSeed = startSeed;
                EndSeed = endSeed;

                for (; ThreadsRunning < Threads; ThreadsRunning++)
                {
                    Thread t = new(() => WorkerThread(print, callback)) { IsBackground = false };
                    t.Start();
                }

                if (print)
                    Console.WriteLine("All threads started.");
            }

            void WorkerThread(bool print, ProfilerCallback? callback)
            {
                Monitor.Enter(ThreadLock);

                while (NextSeed <= EndSeed)
                {
                    int seed = NextSeed++;

                    Monitor.Exit(ThreadLock);

                    Game game = new(SizeX, SizeY, true, seed);

                    while (!game.gameOver)
                        game.MakeMove();

                    if (game.Apple != null)
                    {
                        lock (ThreadLock)
                        {
                            Console.WriteLine($"Seed: { seed }");
                            game.Print(true, true);

                            throw new Exception("A game has failed. See console.");
                        }
                    }

                    Monitor.Enter(ThreadLock);

                    Moves.Add(game.Moves);
                    GamesComplete++;

                    if (print)
                        Console.WriteLine($"{ GamesComplete } / { EndSeed - NextSeed + GamesComplete + ThreadsRunning } games complete.");
                }

                ThreadsRunning--;

                if (print)
                {
                    if (ThreadsRunning == 0)
                    {
                        Console.WriteLine();
                        PrintResults();
                    }
                }

                Monitor.Exit(ThreadLock);

                if (ThreadsRunning == 0 && callback != null)
                    callback();
            }

            public void PrintResults()
            {
                float mean = (float)Moves.Average();

                float standardDeviation = 0;

                foreach (int m in Moves)
                    standardDeviation += MathF.Pow(mean - m, 2);

                standardDeviation = MathF.Sqrt(standardDeviation / Moves.Count);

                Moves.Sort();

                float median;

                if (Moves.Count % 2 == 0)
                    median = ((float)(Moves[Moves.Count / 2] + Moves[Moves.Count / 2 - 1])) / 2;
                else
                    median = Moves[Moves.Count / 2];

                Tuple<string, float>[] data = {
                    new("Mean", mean),
                    new("Standard Deviation", standardDeviation),
                    new("Minimum", Moves.Min()),
                    new("Median", median),
                    new("Maximum", Moves.Max())
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
            }
        }
    }
}