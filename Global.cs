namespace MoleXiangqi
{
    public static class G
    {
        public const int MATE = 10000;
        public const int RULEWIN = MATE - 120;
        public const int WIN = MATE - 200;
        public const int MAX_PLY = 1024;

        public const bool UseDistancePruning = true;
    }
}
