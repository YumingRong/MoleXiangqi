using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    partial class POSITION
    {
        const int MATE_VALUE = 5000;
        public int SearchQuiesce(int alpha, int beta)
        {
            int best = -MATE_VALUE;
            int vl = best;
            List<MOVE> mvs = GenerateCaptures();
            if (mvs.Count == 0)
                return Complex_Evaluate();
            foreach (MOVE mv in mvs)
            {
                Debug.WriteLine(iMove2Coord(mv.sqSrc, mv.sqDst));
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
