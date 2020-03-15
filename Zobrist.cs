using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
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

    public partial class POSITION
    {
        public ulong CalculateZobrist(bool mirror = false)
        {
            ulong zob = 0;
            for (int i = 16; i < 48; i++)
            {
                int sq = sqPieces[i];
                if (sq > 0)
                {
                    int pc = pcSquares[sq];
                    int sq1;
                    sq1 = mirror ? csqMirrorTab[sq] : sq;
                    zob ^= Zobrist.Get(pc, sq1);
                }
            }
            if (sdPlayer == 1)
                zob ^= Zobrist.turn;
            return zob;
        }
    }
}
