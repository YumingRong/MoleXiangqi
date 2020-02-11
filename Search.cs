using System;
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

        public static void DisplayTimerProperties()
        {
            // Display the timer frequency and resolution.
            if (Stopwatch.IsHighResolution)
                Console.WriteLine("Operations timed using the system's high-resolution performance counter.");
            else
                Console.WriteLine("Operations timed using the DateTime class.");

            long frequency = Stopwatch.Frequency;
            Console.WriteLine("  Timer frequency in ticks per second = {0}",
                frequency);
            long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
            Console.WriteLine("  Timer is accurate within {0} nanoseconds",
                nanosecPerTick);
        }

        public override string ToString()
        {
            int totalNodes = QuiesceNodes + PVNodes + CutNodes;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(String.Format("Nodes: total {0}， PV {1}, Cut {2}, Quiesce {3}", totalNodes, QuiesceNodes, PVNodes, CutNodes));
            sb.AppendLine(String.Format("Elapsed time: {0} millisecondN. Nodes per second: {1}", ElapsedTime, totalNodes * 1000 / ElapsedTime));
            sb.AppendLine(String.Format("Cutoffs: {0}", Cutoffs));
            sb.AppendLine(String.Format("Extesions: Check {0}, Capture {1}", CheckExtesions, CaptureExtensions));
            sb.Append("PV line: ");
            return sb.ToString();
        }
    }

    partial class SEARCH
    {
        public POSITION board;
        public STATISTICS stat;
        public List<MOVE> PVLine;
        public List<KeyValuePair<MOVE, int>> rootMoves;

        Stopwatch stopwatch;
        int depth = 0;

        public SEARCH(POSITION pos)
        {
            board = pos;
            stopwatch = new Stopwatch();
            rootMoves = new List<KeyValuePair<MOVE, int>>();
        }

        public MOVE SearchMain(int depthleft)
        {
            stat = new STATISTICS();
            PVLine = new List<MOVE>();
            killers = new MOVE[G.MAX_PLY, 2];
            history = new int[256, 256];
            rootMoves.Clear();

            List<MOVE> moves = board.GenerateMoves();
            foreach (MOVE mv in moves)
            {
                rootMoves.Add(new KeyValuePair<MOVE, int>(mv, 0));
            }
            int vl = 0;

            // 6. 做迭代加深搜索
            for (int i = 1; i <= depthleft; i++)
            {
                Console.WriteLine("---------------------------");
                Console.WriteLine("Search depth {0}", i);
                stopwatch.Start();

                vl = SearchRoot(i);

                PopPVLine();

                if (rootMoves.Count == 1)
                {
                    Console.WriteLine("Single feasible move");
                    break;
                }
                // 10. 搜索到杀棋则终止搜索
                if (vl < -G.WIN || vl > G.WIN)
                    break;

                stopwatch.Stop();
                stat.ElapsedTime += stopwatch.ElapsedMilliseconds;
                Console.WriteLine(stat);
            }
            if (vl < -G.WIN)
                Console.WriteLine("Resign");
            else if (vl > G.WIN)
                Console.WriteLine("Mate in {0} steps", G.MATE - vl);
            return PVLine[0];
        }


        public int SearchRoot(int depthleft)
        {

            int alpha = -G.MATE;
            int beta = G.WIN;
            MOVE mvBest = new MOVE();
            rootMoves.Sort(SortLarge2Small);
            for (int i = 0; i < rootMoves.Count; i++)
            {
                MOVE mv = rootMoves[i].Key;
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2}", mv, alpha, beta);
                board.MakeMove(mv);
                depth++;
                int vl = -SearchPV(-beta, -alpha, depthleft - 1, out List<MOVE> subpv);
                depth--;
                board.UnmakeMove();

                rootMoves[i] = new KeyValuePair<MOVE, int>(mv, vl);

                if (vl > alpha)
                {
                    alpha = vl;
                    mvBest = mv;
                    PVLine.Clear();
                    PVLine.Add(mvBest);
                    PVLine.AddRange(subpv);
                    Console.WriteLine(PopPVLine());
                    if (vl > beta)
                    {
                        stat.Cutoffs++;
                        break;
                    }
                }
            }
            rootMoves.RemoveAll(x => x.Value < -G.WIN);
            Console.WriteLine("Best move {0}, score {1}", mvBest, alpha);
            return alpha;
        }

        public int SearchPV(int alpha, int beta, int depthleft, out List<MOVE> pvs)
        {
            stat.PVNodes++;
            pvs = new List<MOVE>();

            RepititionResult rep = board.Repitition();
            if (rep != RepititionResult.NONE)
                return (int)rep;
            if (depthleft <= 0)
                //静态搜索深度不超过普通搜索的2倍
                return SearchQuiesce(alpha, beta, depth * 2);

            List<MOVE> moves = board.GenerateMoves();
            MOVE mvBest = new MOVE();
            int best = -G.MATE;
            int vl;

            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2} {3}", mv, alpha, beta, best);
                board.MakeMove(mv);
                depth++;
                vl = -SearchPV(-beta, -alpha, depthleft - 1, out List<MOVE> subpv);
                depth--;
                board.UnmakeMove();
                if (vl > best)
                {
                    best = vl;
                    if (vl > beta)
                    {
                        stat.Cutoffs++;
                        mvBest = mv;
                        break;
                    }
                    if (vl > alpha)
                    {
                        alpha = vl;
                        mvBest = mv;
                        pvs.Add(mv);
                        pvs.AddRange(subpv);
                    }
                }
            }

            if (mvBest.sqSrc != 0)
                SetBestMove(mvBest, depth);
            return best;
        }

        public int SearchQuiesce(int alpha, int beta, int depthleft)
        {
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
                best = depth - G.MATE;
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
                int vl = board.Complex_Evaluate();
                if (vl > beta)
                    return vl;
                best = vl;
                alpha = Math.Max(alpha, vl);
                stat.CaptureExtensions += board.captureMoves.Count;
                //如果是将军导致的延伸搜索，则继续寻找连将的着法
                //因为搜索将军着法是一件费时的事情，所以在非连将的情况下，只搜索吃子着法
                if (depthleft > 0 && board.stepList.Count >= 2 && board.stepList[board.stepList.Count - 2].checking > 0)
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
                    board.captureMoves.Sort(SortLarge2Small);
                    foreach (KeyValuePair<MOVE, int> mv_vl in board.captureMoves)
                        selectiveMoves.Add(mv_vl.Key);
                }
                else
                    return vl;
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
                int vl = -SearchQuiesce(-beta, -alpha, depthleft - 1);
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

        string PopPVLine()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MOVE mv in PVLine)
            {
                sb.Append(mv);
                sb.Append(" -- ");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        int SortLarge2Small(KeyValuePair<MOVE, int> a, KeyValuePair<MOVE, int> b)
        {
            return b.Value.CompareTo(a.Value);
        }
    }
}
