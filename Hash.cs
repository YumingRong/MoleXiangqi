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

            static Zobrist()
            {
                Random rnd = new Random(20200115);
                for (int i = 0; i < 9; i++)
                    for (int j = 0; j < 256; j++)
                        table[i, j] = (UInt64)(rnd.NextDouble() * UInt64.MaxValue);
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

        // 置换表结构，置换表信息夹在两个Zobrist校验锁中间，可以防止存取冲突
        struct HashStruct
        {
            public UInt32 ZobristLock;
            public UInt16 move;
            public byte MinDepth,MaxDepth;
            public Int16 alpha, beta;
        }

        Dictionary<UInt32, HashStruct> Trans;
        // 存储置换表局面信息
        void RecordHash(UInt64 key, int vl, int depth, MOVE mv)
        {
            Debug.Assert(vl < G.MATE && vl>-G.MATE);
            if (vl > G.RULEWIN || vl < -G.RULEWIN)
                return;
            if (Trans.TryGetValue((UInt32)(key >> 16), out HashStruct entry))
            {
                if ((UInt32)(key & 0xffff) == entry.ZobristLock)
                {
                    if (entry.MinDepth <depth || entry.alpha> vl)
                    {
                        entry.alpha = (Int16)vl;
                        entry.MinDepth = (byte)depth;
                    }
                }
            }
        }
    }
}
