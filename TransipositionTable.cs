using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    // 置换表结构，置换表信息夹在两个Zobrist校验锁中间，可以防止存取冲突
    struct HashStruct
    {
        public UInt32 ZobristLock;
        public int AlphaDepth
        {
            get { return (int)alphadepth; }
            set { alphadepth = (byte)value; }
        }
        public int BetaDepth
        {
            get { return (int)betadepth; }
            set { betadepth = (byte)value; }
        }
        public int Alpha
        {
            get { return (int)alpha; }
            set { alpha = (byte)value; }
        }
        public int Beta
        {
            get { return (int)beta; }
            set { beta = (byte)value; }
        }
        public MOVE move
        {
            get { return new MOVE(sqSrc, sqDst, 0, 0); }
            set { sqSrc = (byte)( value.sqSrc); sqDst = (byte)(value.sqSrc); }
        }
        byte sqSrc, sqDst;
        byte alphadepth, betadepth;
        short alpha, beta;
    }

    class TransipositionTable
    {
        public int nRead, nReadHit, nWrite, nWriteHit, nWriteCollision;
        // 置换表标志，只用在"RecordHash()"函数中

        public TransipositionTable(int capacity = 512)
        {
            Trans = new Dictionary<UInt64, HashStruct>(1024 * capacity);
        }

        public void Reset()
        {
            Trans.Clear();
            nRead = nReadHit = nWrite = nWriteHit = nWriteCollision = 0;
        }

        Dictionary<UInt64, HashStruct> Trans;
        // 存储置换表局面信息
        public void WriteHash(UInt64 key, int flag, int vl, int depth, MOVE mv)
        {
            Debug.Assert(vl < G.MATE && vl > -G.MATE);
            if ((vl > G.WIN && vl <= G.RULEWIN || vl < -G.WIN && vl >= -G.RULEWIN) && mv.sqSrc == 0)
                return;
            nWrite++;
            if (Trans.TryGetValue(key, out HashStruct entry))
            {
                nWriteHit++;
                if ((UInt32)(key >> 16) == entry.ZobristLock)
                {
                    // 最佳着法是始终覆盖的
                    if (mv.sqDst != 0)
                    {
                        entry.move = mv;
                    }
                    if ((flag & G.HASH_ALPHA) != 0 && (depth > entry.AlphaDepth || vl < entry.Alpha))
                    {
                        entry.Alpha = vl;
                        entry.BetaDepth = depth;
                    }
                    if ((flag & G.HASH_BETA) != 0 && (depth > entry.BetaDepth || vl > entry.Beta))
                    {
                        entry.Beta = vl;
                        entry.BetaDepth = depth;
                    }
                    Trans[key] = entry;
                    return;
                }
                else
                    nWriteCollision++;
            }
            else
            {
                entry.ZobristLock = (UInt32)(key >> 16);
                entry.move = mv;
                entry.Alpha = entry.Beta = 0;
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

        public Nullable<HashStruct> ReadHash(UInt64 key)
        {
            nRead++;
            if (Trans.TryGetValue(key, out HashStruct entry))
            {
                if ((UInt32)(key >> 16) == entry.ZobristLock)
                {
                    nReadHit++;
                    return entry;
                }
            }
            return null;
        }
    }
}