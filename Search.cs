using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    class SEARCH
    {
        public POSITION board;
        int depth = 1;

        public SEARCH(POSITION pos)
        {
            board = pos;
        }

        const int MATE_VALUE = 5000;
        public int SearchQuiesce(int alpha, int beta)
        {
            RepititionResult rep = board.Repitition();
            if (rep != RepititionResult.NONE)
                return (int)rep;

            int best, vl;
            List<MOVE> selectiveMoves = new List<MOVE>();
            int sqCheck = board.stepList.Count >= 2 ? board.stepList[board.stepList.Count - 2].checking : 0;
            if (sqCheck > 0)
            {
                best = depth - MATE_VALUE;
                // 6. 对于被将军的局面，生成全部着法；
                List<MOVE> moves = board.GenerateMoves();
                foreach (MOVE mv in moves)
                {
                    board.MovePiece(mv);
                    int kingpos = board.sqPieces[POSITION.SIDE_TAG(1 - board.sdPlayer)];
                    if (!board.IsLegalMove(sqCheck, kingpos))
                        if (board.CheckedBy(1 - board.sdPlayer) == 0)
                            selectiveMoves.Add(mv);
                    board.UndoMovePiece(mv);
                }
            }
            else
            {
                // 7. 对于未被将军的局面，在生成着法前首先尝试空着(空着启发)，即对局面作评价；
                vl = board.Complex_Evaluate();
                if (vl > beta)
                    return vl;
                best = vl;
                alpha = Math.Max(alpha, vl);
                if (board.captureMoves.Count > 0)
                {
                    board.captureMoves.Sort(delegate (KeyValuePair<MOVE, int> a, KeyValuePair<MOVE, int> b)
                    { return b.Value.CompareTo(a.Value); });
                    foreach (KeyValuePair<MOVE, int> mv_vl in board.captureMoves)
                        selectiveMoves.Add(mv_vl.Key);
                }
                //如果是将军导致的延伸搜索，则继续寻找连将的着法
                //因为搜索将军着法是一件费时的事情，所以在非连将的情况下，只搜索吃子着法
                if (board.stepList.Count >= 3 && board.stepList[board.stepList.Count - 3].checking > 0)
                {
                    List<MOVE> moves = board.GenerateMoves();
                    foreach (MOVE mv in moves)
                    {
                        board.MovePiece(mv);
                        if (board.CheckedBy(board.sdPlayer) > 0)
                            if (!selectiveMoves.Contains(mv))
                                selectiveMoves.Add(mv);
                        board.UndoMovePiece(mv);
                    }
                }

            }
            foreach (MOVE mv in selectiveMoves)
            {
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2} {3}", mv, alpha, beta, best);
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
