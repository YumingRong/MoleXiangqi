using System;
using System.Collections.Generic;

namespace MoleXiangqi
{
    public partial class POSITION
    {
        static class Zobrist
        {
            //帅车车炮炮马马兵兵兵兵兵相相仕仕(将车车炮炮马马卒卒卒卒卒象象士士)
            //兵帅共用，相仕共用。目的除了减少数组体积外，主要是为了提高zobrist的
            //正交性，即尽量避免不同的局面产生相同zobrist的可能性。
            readonly static int[] ZobristTypes = {
             -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
              0, 1, 1, 2, 2, 3, 3, 0, 0, 0, 0, 0, 4, 4, 4, 4,
              5, 6, 6, 7, 7, 8, 8, 5, 5, 5, 5, 5, 4, 4, 4, 4
            };

            readonly static ulong[,] table = new ulong[9, 256];

            static Zobrist()
            {
                Random rnd = new Random(20200115);
                for (int i = 0; i < 9; i++)
                    for (int j = 0; j < 256; j++)
                        table[i, j] = (ulong)(rnd.NextDouble() * UInt64.MaxValue);
            }

            public static ulong Get(int pc, int sq)
            {
                return table[ZobristTypes[pc], sq];
            }
        };
    }

    partial class POSITION
    {
        ulong CalculateZobrist()
        {
            ulong zob = 0;
            for (int x = FILE_LEFT; x < FILE_RIGHT; x++)
                for (int y = RANK_TOP; y < RANK_BOTTOM; y++)
                {
                    int sq = XY2Coord(x, y);
                    int pc = pcSquares[sq];
                    if (pc > 0)
                        zob ^= Zobrist.Get(pc, sq);
                }
            return zob;
        }

    }
}
