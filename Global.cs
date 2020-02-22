namespace MoleXiangqi
{
    public static class G
    {
        public const int MATE = 30000;
        public const int WIN = MATE - 256;
        public const int MAX_PLY = 1024;

        public const bool UseDistancePruning = true;
    }
}
