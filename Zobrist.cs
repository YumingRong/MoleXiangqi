using System;
using System.Collections.Generic;
using System.Diagnostics;

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

            readonly static UInt64[,] table = new UInt64[9, 256];
            public readonly static UInt64 turn;
            static Zobrist()
            {
                Random rnd = new Random(20200115);
                for (int i = 0; i < 9; i++)
                    for (int j = 0; j < 256; j++)
                        table[i, j] = (UInt64)(rnd.NextDouble() * UInt64.MaxValue);
                turn = (UInt64)(rnd.NextDouble() * UInt64.MaxValue);
            }

            public static UInt64 Get(int pc, int sq)
            {
                return table[ZobristTypes[pc], sq];
            }
        };

        UInt64 CalculateZobrist()
        {
            UInt64 zob = 0;
            foreach (int sq in cboard90)
            {
                int pc = pcSquares[sq];
                if (pc > 0)
                    zob ^= Zobrist.Get(pc, sq);
            }
            return zob;
        }
    }
}
