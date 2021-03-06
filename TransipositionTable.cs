﻿#if USE_HASH
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    struct HashStruct
    {
        public int AlphaDepth
        {
            get { return alphadepth; }
            set { alphadepth = (byte)value; }
        }
        public int BetaDepth
        {
            get { return betadepth; }
            set { betadepth = (byte)value; }
        }
        public int Alpha
        {
            get { return alpha; }
            set { alpha = (short)value; }
        }
        public int Beta
        {
            get { return beta; }
            set { beta = (short)value; }
        }
        public MOVE Move
        {
            get 
            { 
                MOVE mv = new MOVE(sqSrc, sqDst, 0, 0);
                mv.checking = checking;
                return mv;
            }
            set { 
                sqSrc = (byte)( value.sqSrc); 
                sqDst = (byte)(value.sqDst);
                checking = value.checking;
            }
        }
        byte sqSrc, sqDst;
        byte alphadepth, betadepth;
        short alpha, beta;
        bool checking;
    }

    class TransipositionTable
    {
        public int nRead, nReadHit, nWrite, nWriteHit;

        public TransipositionTable(int capacity = 512)
        {
            Trans = new Dictionary<ulong, HashStruct>(1024 * capacity);
        }

        public void Reset()
        {
            Trans.Clear();
            nRead = nReadHit = nWrite = nWriteHit = 0;
        }

        readonly Dictionary<ulong, HashStruct> Trans;

        public void WriteHash(ulong key, int flag, int vl, int depth, MOVE mv)
        {
            Debug.Assert(vl < G.MATE && vl > -G.MATE);
            if (vl > G.WIN && vl <= G.RULEWIN || vl < -G.WIN && vl >= -G.RULEWIN)
                return;
            //Mate value is accurate regardless of the searching depth.
            if (vl > G.WIN || vl < -G.WIN)
                depth = G.MAX_PLY;
            nWrite++;
            if (Trans.TryGetValue(key, out HashStruct entry))
            {
                nWriteHit++;
                if (mv.sqDst != 0)
                {
                    entry.Move = mv;
                }
                if ((flag & G.HASH_ALPHA) != 0 && (depth > entry.AlphaDepth))
                {
                    entry.Alpha = vl;
                    entry.BetaDepth = depth;
                }
                if ((flag & G.HASH_BETA) != 0 && (depth > entry.BetaDepth))
                {
                    entry.Beta = vl;
                    entry.BetaDepth = depth;
                }
                Trans[key] = entry;
                return;
            }
            else
            {
                entry.Move = mv;
                entry.Alpha = G.MATE;
                entry.Beta = -G.MATE;
                if ((flag & G.HASH_ALPHA) != 0)
                {
                    entry.AlphaDepth = depth;
                    entry.Alpha = vl;

                }
                if ((flag & G.HASH_BETA) != 0)
                {
                    entry.BetaDepth = depth;
                    entry.Beta = vl;
                }
                Trans.Add(key, entry);
            }
        }

        public HashStruct ReadHash(ulong key)
        {
            Debug.Assert(key != 0);
            nRead++;
            if (Trans.TryGetValue(key, out HashStruct entry))
            {
                nReadHit++;
                return entry;
            }
            return new HashStruct();
        }

        public void PrintStatus()
        {
            Debug.WriteLine($"Write No. {nWrite}, hit {nWriteHit}.Dictionary items {Trans.Count}.");
            Debug.WriteLine($"Read No. {nRead}, hit {nReadHit}.");
        }
    }
}
#endif