using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    partial class POSITION
    {
        // 判断棋子是否在棋盘中的数组
        public readonly static bool[] IN_BOARD = {
                false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

        // 判断棋子是否在九宫的数组
        readonly static bool[] IN_FORT = {
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
  false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false,
  false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false,
  false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false,
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
  false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false,
  false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false,
  false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false,
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
  false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
};

        // 是否未过河
        static readonly bool[,] HOME_HALF =
        {
            {
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
            },
            {
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, true, true, true, true, true, true, true, true, true, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
            }
        };

        // 判断步长是否符合特定走法的数组，1=帅(将)，2=仕(士)，3=相(象)
        readonly static int[] ccLegalSpan = {
                       0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 3, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 2, 1, 2, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 2, 1, 2, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 3, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0
        };

        // 根据步长判断马是否蹩腿的数组
        readonly static int[] ccKnightPin = {
                              0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,-16,  0,-16,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0, -1,  0,  0,  0,  1,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0, -1,  0,  0,  0,  1,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0, 16,  0, 16,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
          0,  0,  0,  0,  0,  0,  0
        };

        // 帅(将)的步长
        static readonly int[] ccKingDelta = { -0x10, +0x10, -0x01, +0x01 };
        // 仕(士)的步长
        static readonly int[] ccGuardDelta = { -0x11, +0x11, -0x0f, +0x0f };
        // 马的步长，以帅(将)的步长作为马腿
        static readonly int[,] ccKnightDelta = { { -33, -31 }, { 31, 33 }, { -18, 14 }, { -14, 18 } };
        // 马被将军的步长，以仕(士)的步长作为马腿
        static readonly int[,] ccKnightCheckDelta = { { -33, -18 }, { 18, 33 }, { -31, -14 }, { 14, 31 } };
        //[pin, direction]，用来判断棋子运动方向受不受牵制
        static readonly bool[,] ccPinDelta = { { false, false, false, false }, { false, false, true, true, }, { true, true, false, false }, { true, true, true, true } };
        static readonly int[] cnBishopMoveTab = { -0x22, -0x1e, +0x1e, +0x22 };
        static readonly int[] cnKnightMoveTab = { -0x21, -0x1f, -0x12, -0x0e, +0x0e, +0x12, +0x1f, +0x21 };


        static readonly int[,] csqKingMoves = new int[256, 5];
        static readonly int[,] csqAdvisorMoves = new int[256, 5];
        static readonly int[,] csqBishopMoves = new int[256, 5];
        static readonly int[,] csqKnightMoves = new int[256, 9];
        static readonly int[,] csqKnightPins = new int[256, 9];

        public readonly static int[] cboard90 = new int[90];
        readonly static int[] cboard256 = new int[256];

        void InitPreGen()
        {
            int i, n, sqSrc, sqDst;
            n = 0;
            for (int x = FILE_LEFT; x <= FILE_RIGHT; x++)
                for (int y = RANK_TOP; y <= RANK_BOTTOM; y++)
                {
                    int sq = XY2Coord(x, y);
                    cboard90[n] = sq;
                    cboard256[sq] = n;
                    n++;
                }

            for (n = 0; n < 2; n++)
                foreach (int sq in cboard90)
                    AttackMap[n, sq] = new List<int>(8);

            // 接下来生成着法预生成数组，连同将军预判数组
            for (sqSrc = 0; sqSrc < 256; sqSrc++)
            {
                if (IN_BOARD[sqSrc])
                {
                    // 生成帅(将)的着法预生成数组
                    n = 0;
                    for (i = 0; i < 4; i++)
                    {
                        sqDst = sqSrc + ccKingDelta[i];
                        if (IN_FORT[sqDst])
                        {
                            csqKingMoves[sqSrc, n] = sqDst;
                            n++;
                        }
                    }
                    csqKingMoves[sqSrc, n] = 0;
                    // 生成仕(士)的着法预生成数组
                    n = 0;
                    for (i = 0; i < 4; i++)
                    {
                        sqDst = sqSrc + ccGuardDelta[i];
                        if (IN_FORT[sqDst])
                        {
                            csqAdvisorMoves[sqSrc, n] = sqDst;
                            n++;
                        }
                    }
                    csqAdvisorMoves[sqSrc, n] = 0;
                    // 生成相(象)的着法预生成数组，包括象眼数组
                    n = 0;
                    for (i = 0; i < 4; i++)
                    {
                        sqDst = sqSrc + cnBishopMoveTab[i];
                        if (IN_BOARD[sqDst] && SAME_HALF(sqSrc, sqDst))
                        {
                            csqBishopMoves[sqSrc, n] = sqDst;
                            n++;
                        }
                    }
                    csqBishopMoves[sqSrc, n] = 0;
                    // 生成马的着法预生成数组，包括马腿数组
                    n = 0;
                    for (i = 0; i < 8; i++)
                    {
                        sqDst = sqSrc + cnKnightMoveTab[i];
                        if (IN_BOARD[sqDst])
                        {
                            csqKnightMoves[sqSrc, n] = sqDst;
                            csqKnightPins[sqSrc, n] = KNIGHT_PIN(sqSrc, sqDst);
                            n++;
                        }
                    }
                    csqKnightMoves[sqSrc, n] = 0;
                }
            }
        }
    }

}