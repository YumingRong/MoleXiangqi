using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace MoleXiangqi
{
    // 置换表结构，置换表信息夹在两个Zobrist校验锁中间，可以防止存取冲突
    struct HashStruct
    {
        public UInt32 ZobristLock;
        private byte sqSrc, sqDst;
        public byte AlphaDepth, BetaDepth;
        public short alpha, beta;
        public MOVE move
        {
            get { return new MOVE(sqSrc, sqDst, 0, 0); }
            set { sqSrc = (byte)( value.sqSrc); sqDst = (byte)(value.sqSrc); }
        }
    }

    class TransipositionTable
    {
        public int nRead, nReadHit, nWrite, nWriteHit, nWriteCollision;
        // 置换表标志，只用在"RecordHash()"函数中
        const int HASH_BETA = 1;
        const int HASH_ALPHA = 2;
        const int HASH_PV = HASH_ALPHA | HASH_BETA;

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
                    if ((flag & HASH_ALPHA) != 0 && (depth > entry.AlphaDepth || vl < entry.alpha))
                    {
                        entry.alpha = (short)vl;
                        entry.BetaDepth = (byte)depth;
                    }
                    if ((flag & HASH_BETA) != 0 && (depth > entry.BetaDepth || vl > entry.beta))
                    {
                        entry.beta = (short)vl;
                        entry.BetaDepth = (byte)depth;
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
                entry.alpha = entry.beta = 0;
                if ((flag & HASH_ALPHA) != 0)
                {
                    entry.AlphaDepth = (byte)depth;
                    entry.alpha = (short)vl;

                }
                if ((flag & HASH_BETA) != 0)
                {
                    entry.BetaDepth = (byte)depth;
                    entry.beta = (short)vl;
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