using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    class SEARCH
    {
        public POSITION board;
        int depth = 0;

        public SEARCH(POSITION pos)
        {
            board = pos;
        }

        //const int MATE_VALUE = 5000;
        public int SearchQuiesce(int alpha, int beta)
        {
            int best;
            int vl;
            
            if (board.Checked())
            {
                List<MOVE> moves = board.GenerateMoves();
                foreach (MOVE mv in moves)
                {
                    board.MakeMove(mv);
                    if (!board.Checked())
                    {
                        depth++;
                        vl = -SearchQuiesce(-beta, -alpha);
                        board.UnmakeMove();
                        depth--;

                    }

                }
            }
            else
            {

            }
            // 7. 对于未被将军的局面，在生成着法前首先尝试空着(空着启发)，即对局面作评价；
            vl = board.Complex_Evaluate();
            if (vl > beta)
                return vl;
            best = vl;
            alpha = Math.Max(alpha, vl);

            board.captureMoves.Sort(delegate (KeyValuePair<MOVE, int> a, KeyValuePair<MOVE, int> b)
            { return b.Value.CompareTo(a.Value); });
            if (board.captureMoves.Count == 0)
                return best;
            foreach (KeyValuePair<MOVE, int> mv_vl in board.captureMoves)
            {
                MOVE mv = mv_vl.Key;
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2} {3}",mv, alpha, beta, best);
                board.MakeMove(mv);
                depth++;
                vl = -SearchQuiesce(-beta, -alpha);
                board.UnmakeMove();
                depth--;
                //Debug.Write(new string('\t', depth));
                //Debug.WriteLine("{0} {1}", mv, best);
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
