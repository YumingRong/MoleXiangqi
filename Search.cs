using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MoleXiangqi
{
    public struct STATISTIC
    {
        public int QuiesceNodes, PVNodes, CutNodes;
        public long ElapsedTime;
        public int Cutoffs, PVChanged;
        public int CaptureExtensions, CheckExtesions;

        public static void DisplayTimerProperties()
        {
            // Display the timer frequency and resolution.
            if (Stopwatch.IsHighResolution)
                Console.WriteLine("Operations timed using the system's high-resolution performance counter.");
            else
                Console.WriteLine("Operations timed using the DateTime class.");

            long frequency = Stopwatch.Frequency;
            Console.WriteLine($"  Timer frequency in ticks per second = {frequency}");
            long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
            Console.WriteLine($"  Timer is accurate within {nanosecPerTick} nanoseconds");
        }

        public override string ToString()
        {
            int totalNodes = QuiesceNodes + PVNodes + CutNodes;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Nodes: total {totalNodes}， PV {PVNodes}, Cut {CutNodes}, Quiesce {QuiesceNodes}");
            sb.AppendLine($"Elapsed time: {ElapsedTime} millisecond. Nodes per second: {totalNodes * 1000 / ElapsedTime}");
            sb.AppendLine($"Cutoffs: {Cutoffs}, PV re-searched: {PVChanged}");
            sb.AppendLine($"Extesions: Check {CheckExtesions}, Capture {CaptureExtensions}");
            return sb.ToString();
        }
    }

    partial class POSITION
    {
        public STATISTIC stat;
        public List<MOVE> PVLine;
        public List<KeyValuePair<MOVE, int>> rootMoves;

        internal Stopwatch stopwatch;
        internal int depth = 0, maxDepth;

        public void InitSearch()
        {
            stopwatch = new Stopwatch();
            rootMoves = new List<KeyValuePair<MOVE, int>>();

            //临时，需删除           
            killers = new MOVE[G.MAX_PLY, 2];

        }


        public MOVE SearchMain(int depthleft)
        {
            stat = new STATISTIC();
            PVLine = new List<MOVE>();
            killers = new MOVE[G.MAX_PLY, 2];
            history = new int[40, 256];
            rootMoves = InitRootMoves();

            int vl = 0;

            // 6. 做迭代加深搜索
            for (int maxDepth = 1; maxDepth <= depthleft; maxDepth++)
            {
                Console.WriteLine("---------------------------");
                Console.WriteLine("Search depth {0}", maxDepth);
                stopwatch.Start();

                vl = SearchRoot(maxDepth);

                PopPVLine();

                if (rootMoves.Count == 1)
                {
                    Console.WriteLine("Single feasible move");
                    break;
                }
                stopwatch.Stop();
                stat.ElapsedTime += stopwatch.ElapsedMilliseconds;
                Console.WriteLine(stat);

                // 10. 搜索到杀棋则终止搜索
                if (vl < -G.WIN || vl > G.WIN)
                    break;
            }
            if (vl < -G.WIN)
                Console.WriteLine("Resign");
            else if (vl > G.WIN)
                Console.WriteLine("MATE in {0} steps!", G.MATE - vl);
            return PVLine[0];
        }


        public int SearchRoot(int depthleft)
        {
            int alpha = -G.WIN;
            int beta = G.WIN;
            MOVE mvBest = new MOVE();
            List<MOVE> subpv = null;
            for (int i = 0; i < rootMoves.Count; i++)
            {
                MOVE mv = rootMoves[i].Key;
                Debug.Write(new string('\t', depth));
                Debug.WriteLine($"{mv} {alpha}, {beta}");
                MakeMove(mv);
                depth++;
                int vl;
                vl = -SearchPV(-beta, -alpha, depthleft - 1, out subpv);
                //if (alpha == -G.WIN)
                //    vl = -SearchPV(-beta, -alpha, depthleft - 1, out subpv);
                //else
                //{
                //    vl = -SearchCut(-alpha, depthleft - 1);
                //    if (vl > alpha)
                //    {
                //        stat.PVChanged++;
                //        vl = -SearchPV(-beta, -alpha, depthleft - 1, out subpv);
                //    }
                //}
                depth--;
                UnmakeMove();

                rootMoves[i] = new KeyValuePair<MOVE, int>(mv, vl);

                if (vl > alpha)
                {
                    alpha = vl;
                    mvBest = mv;
                    PVLine.Clear();
                    PVLine.Add(mvBest);
                    PVLine.AddRange(subpv);
                    Console.WriteLine("PV:" + PopPVLine());
                    if (vl > beta)
                    {
                        stat.Cutoffs++;
                        break;
                    }
                }
            }
            rootMoves.RemoveAll(x => x.Value < -G.WIN);
            rootMoves.Sort(SortLarge2Small);
            Console.WriteLine("Root move\tScore");
            foreach (KeyValuePair<MOVE, int> mv_vl in rootMoves)
                Console.WriteLine($"{mv_vl.Key}\t{mv_vl.Value}");
            Console.WriteLine($"Best move {mvBest}, score {alpha}");
            return alpha;
        }

        public int SearchPV(int alpha, int beta, int depthleft, out List<MOVE> pvs)
        {
            pvs = new List<MOVE>();
            if (depthleft <= 0)
            {
                if (stepList[stepList.Count - 1].checking > 0)
                    //被照将时，推迟进入静态搜索
                    depthleft++;
                else
                    return SearchQuiesce(alpha, beta, 0);
            }

            stat.PVNodes++;
            if (stepList[stepList.Count - 1].halfMoveClock >= 120)
                return 0;
            RepititionResult rep = Repitition();
            if (rep != RepititionResult.NONE)
                return (int)rep;

            MOVE mvBest = new MOVE();
            int best = -G.MATE;
            int vl;
            bool bResearch = false;
            List<MOVE> subpv = null;
            IEnumerable<MOVE> moves = GetNextMove(7);
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', depth));
                Debug.WriteLine($"{mv} {alpha}, {beta}, {best}");
                MakeMove(mv);
                depth++;
                vl = -SearchPV(-beta, -alpha, depthleft - 1, out subpv);
                //if (best == -G.MATE)
                //    vl = -SearchPV(-beta, -alpha, depthleft - 1, out subpv);
                //else
                //{
                //    vl = -SearchCut(-alpha, depthleft - 1);
                //    if (vl > alpha && vl < beta)
                //    {
                //        Debug.WriteLine("Re-search");
                //        vl = -SearchPV(-beta, -alpha, depthleft - 1, out subpv);
                //        stat.PVChanged++;
                //        bResearch = true;
                //    }
                //}
                depth--;
                UnmakeMove();
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
                        if (bResearch)
                            pvs.RemoveAt(pvs.Count - 1);
                    }
                    pvs.Add(mv);
                    pvs.AddRange(subpv);
                }
            }

            if (mvBest.sqSrc != 0)
                SetBestMove(mvBest, best);
            return best;
        }

        int SearchCut(int beta, int depthleft)
        {
            if (depthleft <= 0)
            {
                if (stepList[stepList.Count - 1].checking > 0)
                    //被照将时，推迟进入静态搜索
                    depthleft++;
                else
                    return SearchQuiesce(beta - 1, beta, 0);
            }

            stat.CutNodes++;
            if (stepList[stepList.Count - 1].halfMoveClock >= 120)
                return 0;
            RepititionResult rep = Repitition();
            if (rep != RepititionResult.NONE)
                return (int)rep;

            int best = -G.MATE;
            IEnumerable<MOVE> moves = GetNextMove(7);
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', depth));
                Debug.WriteLine($"{mv} {beta - 1}, {beta}, {best}");
                MakeMove(mv);
                depth++;
                int vl = -SearchCut(1 - beta, depthleft - 1);
                depth--;
                UnmakeMove();

                if (vl > best)
                {
                    best = vl;
                    if (vl > beta)
                    {
                        stat.Cutoffs++;
                        //吃送吃的子不记录为推荐着法
                        if (mv.pcDst != stepList[stepList.Count - 1].move.pcSrc)
                            SetBestMove(mv, best);
                        return vl;
                    }
                }
            }
            return best;
        }

        public int SearchQuiesce(int alpha, int beta, int qdepth)
        {
            stat.QuiesceNodes++;
            int sqCheck = stepList[stepList.Count - 1].checking;
            int best;
            IEnumerable<MOVE> moves;
            //偶数层是主动方，奇数层是被动方
            if (qdepth % 2 == 0)
            {
                best = Simple_Evaluate();
                //if (depth > maxDepth)
                //    return best;
                if (best > beta)
                {
                    stat.Cutoffs++;
                    return best;
                }
                if (best > alpha)
                    alpha = best;
                //only extend check and capture
                if (sqCheck == 0)
                    moves = GetNextMove(3);
                else
                    moves = GetNextMove(7);
            }
            else
            {
                if (sqCheck > 0)    //避免长照
                {
                    RepititionResult rep = Repitition();
                    if (rep != RepititionResult.NONE)
                        return (int)rep;
                    best = depth - G.MATE;
                }
                else
                {
                    best = Simple_Evaluate();
                    if (best > beta)
                    {
                        stat.Cutoffs++;
                        return best;
                    }
                    if (best > alpha)
                        alpha = best;
                }
                //only extend evade and re-capture
                if (sqCheck > 0)
                    moves = GetNextMove(7);
                else
                    moves = GetNextMove(2);
            }
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', depth));
                Debug.WriteLine($"{mv} {alpha}, {beta}, {best}, {depth}");
                MakeMove(mv);
                if (qdepth % 2 == 0)
                {
                    if (stepList[stepList.Count -1 ].checking > 0)
                        stat.CheckExtesions++;
                    else
                        stat.CaptureExtensions++;
                }
                depth++;
                int vl = -SearchQuiesce(-beta, -alpha, qdepth + 1);
                depth--;
                UnmakeMove();
                if (vl > beta)
                {
                    stat.Cutoffs++;
                    //吃送吃的子不记录为推荐着法
                    if (mv.pcDst != stepList[stepList.Count - 1].move.pcSrc)
                        SetBestMove(mv, best);
                    return vl;
                }
                if (vl > best)
                {
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
