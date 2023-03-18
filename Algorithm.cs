namespace AutoSnake3
{
    public partial class Snake
    {
        public partial class Game
        {
            Cell? AlgorithmHead;

            void InitilizeAlgorithm()
            {
                if (SizeX % 2 == 1 && SizeY % 2 == 1)
                    throw new Exception("SizeX or SizeY must be an even number for algorithm to work");

                else if (SizeX % 2 == 1)
                {
                    for (int y = 0; y < SizeY; y += 2)
                    {
                        for (int x = 1; x < SizeX; x++)
                        {
                            Matrix[x, y].NextDirection = Direction.Left;
                            Matrix[x, y + 1].NextDirection = Direction.Right;
                        }

                        Matrix[SizeX - 1, y + 1].NextDirection = Direction.Down;
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
                            Matrix[x + 1, y].NextDirection = Direction.Up;
                        }

                        Matrix[x + 1, SizeY - 1].NextDirection = Direction.Left;
                        Matrix[x, 1].NextDirection = Direction.Left;
                    }

                    Matrix[SizeX - 1, 0].NextDirection = Direction.Up;
                    Matrix[0, 1].NextDirection = Direction.Down;

                    for (int x = 0; x < SizeX - 1; x++)
                        Matrix[x, 0].NextDirection = Direction.Right;
                }

                AlgorithmHead = Head;
            }

            void CalculatePath()
            {
                throw new NotImplementedException();
            }
        }
    }
}