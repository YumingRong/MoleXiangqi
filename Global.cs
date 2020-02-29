namespace MoleXiangqi
{
    public static class G
    {
        public const int MATE = 10000;
        public const int RULEWIN = MATE - 120;
        public const int WIN = MATE - 200;
        public const int MAX_PLY = 1024;
        public const int MAX_Depth = 3;

        public const bool UseHash = true;
        public const bool UseFutilityPruning = true;

        public const int FutilityMargin = 10;

        public const int HASH_BETA = 1;
        public const int HASH_ALPHA = 2;
        public const int HASH_PV = HASH_ALPHA | HASH_BETA;
    }
}
