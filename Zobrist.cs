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

            readonly static ulong[,] table = new ulong[9, 256];
            public readonly static ulong turn;
            static Zobrist()
            {
                Random rnd = new Random(20200115);
                for (int i = 0; i < 9; i++)
                    for (int j = 0; j < 256; j++)
                        table[i, j] = (ulong)(rnd.NextDouble() * ulong.MaxValue);
                turn = (ulong)(rnd.NextDouble() * ulong.MaxValue);
            }

            public static ulong Get(int pc, int sq)
            {
                return table[ZobristTypes[pc], sq];
            }
        };

        ulong CalculateZobrist()
        {
            ulong zob = 0;
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
