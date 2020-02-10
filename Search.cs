﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MoleXiangqi
{
    public struct STATISTICS
    {
        public int QuiesceNodes, PVNodes, CutNodes;
        public long ElapsedTime;
        public long Cutoffs;
        public int CaptureExtensions, CheckExtesions;

        //public STATISTICS()
        //{
        //    QuiesceNodes = PVNodes = CutNodes = 0;
        //    ElapsedTime = 0;
        //}

        public static void DisplayTimerProperties()
        {
            // Display the timer frequency and resolution.
            if (Stopwatch.IsHighResolution)
            {
                Console.WriteLine("Operations timed using the system's high-resolution performance counter.");
            }
            else
            {
                Console.WriteLine("Operations timed using the DateTime class.");
            }

            long frequency = Stopwatch.Frequency;
            Console.WriteLine("  Timer frequency in ticks per second = {0}",
                frequency);
            long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
            Console.WriteLine("  Timer is accurate within {0} nanoseconds",
                nanosecPerTick);
        }

        public override string ToString()
        {
            DisplayTimerProperties();
            int totalNodes = QuiesceNodes + PVNodes + CutNodes; 
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(String.Format("Nodes: total {0}， PV {1}, Cut {2}, Quiesce {3}", totalNodes, QuiesceNodes, PVNodes, CutNodes));
            sb.AppendLine(String.Format("Elapsed time: {0} millisecondN. Nodes per second: {1}", ElapsedTime, totalNodes * 1000 / ElapsedTime));
            sb.AppendLine(String.Format("Cutoffs: {0}", Cutoffs));
            sb.AppendLine(String.Format("Extesions: Check {0}, Capture {1}", CheckExtesions, CaptureExtensions));

            return sb.ToString();
        }
    }

    class SEARCH
    {
        public POSITION board;
        int depth = 0;
        public int quiesceNodes = 0;   //for performance measurement
        STATISTICS stat;

        public SEARCH(POSITION pos)
        {
            board = pos;
            stat = new STATISTICS();
        }

        const int MATE = 5000;

        public MOVE SearchRoot(int depthleft)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int alpha = -MATE;
            int beta = MATE - 100;
            MOVE mvBest = new MOVE();
            List<MOVE> moves = board.GenerateMoves();
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2}", mv, alpha, beta);
                board.MakeMove(mv);
                depth++;
                int vl = -SearchPV(-beta, -alpha, depthleft - 1);
                depth--;
                board.UnmakeMove();
                
                if (vl > alpha)
                {
                    alpha = vl;
                    mvBest = mv;

                    if (vl > beta)
                    {
                        stat.Cutoffs++;
                        break;
                    }
                }
            }
            stopwatch.Stop();
            stat.ElapsedTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine(stat);
            return mvBest;
        }

        public int SearchPV(int alpha, int beta, int depthleft)
        {
            int best = -MATE;
            int vl;
            stat.PVNodes++;
            if (depthleft <= 0)
                return SearchQuiesce(alpha, beta);
            List<MOVE> moves = board.GenerateMoves();
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2} {3}", mv, alpha, beta, best);
                board.MakeMove(mv);
                depth++;
                vl = -SearchPV(-beta, -alpha, depthleft - 1);
                depth--;
                board.UnmakeMove();
                if (vl > beta)
                {
                    stat.Cutoffs++;
                    return vl;
                }
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
            stat.QuiesceNodes++;

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
                //对于未被将军的局面，在生成着法前首先对局面作评价；
                vl = board.Complex_Evaluate();
                if (vl > beta)
                    return vl;
                best = vl;
                alpha = Math.Max(alpha, vl);
                stat.CaptureExtensions += board.captureMoves.Count;
                //如果是将军导致的延伸搜索，则继续寻找连将的着法
                //因为搜索将军着法是一件费时的事情，所以在非连将的情况下，只搜索吃子着法
                if (board.stepList.Count >= 2 && board.stepList[board.stepList.Count - 2].checking > 0)
                {
                    List<MOVE> moves = board.GenerateMoves();
                    foreach (MOVE mv in moves)
                    {
                        board.MovePiece(mv);
                        //选择不重复，未对将并将军对方的着法
                        if (mv.pcDst == 0 && !board.KingsFace2Face() && board.CheckedBy(board.sdPlayer) > 0)
                        {
                            //给送吃的着法打稍低分
                            int score = board.attackMap[1 - board.sdPlayer, mv.sqDst] == 0 ? 30 : 10;
                            //能够几个子轮流照将就不要老拿一个子照将
                            if (mv.pcSrc != board.stepList[board.stepList.Count - 2].move.pcSrc)
                                score += 10;
                            board.captureMoves.Add(new KeyValuePair<MOVE, int>(mv, score));
                            stat.CheckExtesions++;
                        }
                        board.UndoMovePiece(mv);
                    }
                }
                if (board.captureMoves.Count > 0)
                {
                    board.captureMoves.Sort(delegate (KeyValuePair<MOVE, int> a, KeyValuePair<MOVE, int> b)
                    { return b.Value.CompareTo(a.Value); });
                    foreach (KeyValuePair<MOVE, int> mv_vl in board.captureMoves)
                        selectiveMoves.Add(mv_vl.Key);
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
                vl = -SearchQuiesce(-beta, -alpha);
                depth--;
                board.UnmakeMove();
                //Debug.Write(new string('\t', depth));
                //Debug.WriteLine("{0} {1}", mv, best);
                if (vl > best)
                {
                    if (vl > beta)
                    {
                        stat.Cutoffs++;
                        return vl;
                    }
                    best = vl;
                    alpha = Math.Max(alpha, vl);
                }

            }
            return best;
        }
    }
}
