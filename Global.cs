namespace MoleXiangqi
{
    public static class G
    {
        public const int MATE = 10000;
        public const int RULEWIN = MATE - 120;
        public const int WIN = MATE - 200;
        public const int MAX_PLY = 1024;
        public const int MAX_Depth = 4;
        public const int MAX_QUEISCE_DEPTH = 30;

        public const int FutilityMargin = 20;
        public const int NullOKMargin = 20;
        public const int NullSafeMargin = 40;
        public const int NullReduction = 3;
        public const int VerReduction = 5;
        public const int NullDepth = 2;    // 空着裁剪的深度
        public const int IIDDepth = 3;
        public const int IIDReduction = 2;

        public const int HASH_BETA = 1;
        public const int HASH_ALPHA = 2;
        public const int HASH_PV = HASH_ALPHA | HASH_BETA;
    }
}
