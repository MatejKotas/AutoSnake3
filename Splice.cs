namespace AutoSnake3
{
    public partial class Snake
    {
        public partial class Game
        {
            internal struct Splice
            {
                internal Cell? Origin;

                internal Direction OldDirection;
                internal Direction NewDirection;

                internal Splice(Cell origin, Direction newDirection)
                {
                    OldDirection = origin.NextDirection;
                    NewDirection = newDirection;

                    if (origin.NextDirection == newDirection || origin.Move(newDirection)!.Previous.DistanceTo(origin.Next) > 1)
                    {
                        Origin = null;

                        return;
                    }

                    Origin = origin;

                    origin.Move(newDirection)!.Previous.NextDirection = ReverseDirection(newDirection);

                    origin.NextDirection = newDirection;
                }

                internal void Reset()
                {
                    Origin!.Next.Move(OldDirection)!.NextDirection = ReverseDirection(OldDirection);
                    Origin.NextDirection = OldDirection;

                    Origin = null;
                }
            }
        }
    }
}
