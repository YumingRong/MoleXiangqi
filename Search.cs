using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    class SEARCH
    {
        public POSITION board;
        int depth = 1;
        public int quiesceNodes = 0;   //for performance measurement

        public SEARCH(POSITION pos)
        {
            board = pos;
        }

        const int MATE = 5000;

        public MOVE SearchMain()
        {
            int alpha = -MATE;
            int beta = MATE - 100;
            MOVE mvBest = new MOVE();
            List<MOVE> moves = board.GenerateMoves();
            foreach(MOVE mv in moves)
            {
                board.MakeMove(mv);
                int vl = SearchPV(alpha, beta, 1);
                board.UnmakeMove();
                if (vl > beta)
                    return mv;
                if (vl > alpha)
                {
                    alpha = vl;
                    mvBest = mv;
                }
            }
            return mvBest;
        }

        public int SearchPV(int alpha, int beta, int depthleft)
        {
            int best = -MATE;
            int vl;
            if (depthleft <= 0)
                return SearchQuiesce(alpha, beta);
            List<MOVE> moves = board.GenerateMoves();
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2} {3}", mv, alpha, beta, best);
                board.MakeMove(mv);
                vl = -SearchPV(-beta, -alpha, depthleft - 1);
                board.UnmakeMove();
                if (vl > beta)
                    return vl;
                if (vl > best)
                {
                    best = vl;
                    if (vl > alpha)
                        alpha = vl;
                }
            }
            return best;
        }

        public int SearchQuiesce(int alpha, int beta)
        {
            // 1. 杀棋步数裁剪；
            int vl = depth - MATE;
            if (vl >= beta)
            {
                return vl;
            }
            if (board.stepList[board.stepList.Count - 1].halfMoveClock >= 120)
                return 0;
            RepititionResult rep = board.Repitition();
            if (rep != RepititionResult.NONE)
                return (int)rep;
            quiesceNodes++;

            int best;
            List<MOVE> selectiveMoves = new List<MOVE>();
            int sqCheck = board.stepList[board.stepList.Count - 1].checking;
            if (sqCheck > 0)
            {
                best = depth - MATE;
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
                if (board.stepList.Count >= 2 && board.stepList[board.stepList.Count - 2].checking > 0)
                {
                    List<MOVE> moves = board.GenerateMoves();
                    foreach (MOVE mv in moves)
                    {
                        board.MovePiece(mv);
                        if (!board.KingsFace2Face() && board.CheckedBy(board.sdPlayer) > 0)
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
                //// 如果移动后被将军了，那么着法是非法的，撤消该着法
                //if (board.CheckedBy(1 - board.sdPlayer) > 0)
                //{
                //    board.UnmakeMove();
                //    continue;
                //}

                depth++;
                if (mv.ToString() == "H8-H9")
                    Debug.WriteLine("Stop line");
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
