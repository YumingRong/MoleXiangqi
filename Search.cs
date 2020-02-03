using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    partial class POSITION
    {
        //const int MATE_VALUE = 5000;
        public int SearchQuiesce(int alpha, int beta)
        {
            int best;
            int vl;
            // 7. 对于未被将军的局面，在生成着法前首先尝试空着(空着启发)，即对局面作评价；
            vl = Complex_Evaluate();
            if (vl > beta)
                return vl;
            best = vl;
            alpha = Math.Max(alpha, vl);

            captureMoves.Sort(delegate (KeyValuePair<MOVE, int> a, KeyValuePair<MOVE, int> b)
            { return b.Value.CompareTo(a.Value); });
            if (captureMoves.Count == 0)
                return best;
            foreach (KeyValuePair<MOVE, int> mv_vl in captureMoves)
            {
                MOVE mv = mv_vl.Key;
                Debug.WriteLine(mv);
                MakeMove(mv);
                vl = -SearchQuiesce(-beta, -alpha);
                UnmakeMove();
                if (vl > best)
                {
                    if (vl > beta)
                        return vl;
                    best = vl;
                    alpha = Math.Max(alpha, vl);
                }
            }
            return best;
        }
    }
}
