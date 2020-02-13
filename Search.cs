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
            sb.AppendLine($"Cutoffs: {Cutoffs}");
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
        internal int depth = 0;

        public void InitSearch()
        {
            stopwatch = new Stopwatch();
            rootMoves = new List<KeyValuePair<MOVE, int>>();
        }

        public MOVE SearchMain(int depthleft)
        {
            stat = new STATISTIC();
            PVLine = new List<MOVE>();
            killers = new MOVE[G.MAX_PLY, 2];
            history = new int[256, 256];
            rootMoves.Clear();

            foreach (MOVE mv in GetNextMove())
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
            int alpha = -G.WIN;
            int beta = G.WIN;
            MOVE mvBest = new MOVE();
            for (int i = 0; i < rootMoves.Count; i++)
            {
                MOVE mv = rootMoves[i].Key;
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2}", mv, alpha, beta);
                MakeMove(mv);
                depth++;
                int vl = -SearchPV(-beta, -alpha, depthleft - 1, out List<MOVE> subpv);
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
            stat.PVNodes++;
            pvs = new List<MOVE>();

            if (stepList[stepList.Count - 1].halfMoveClock >= 120)
                return 0;
            RepititionResult rep = Repitition();
            if (rep != RepititionResult.NONE)
                return (int)rep;
            if (depthleft <= 0)
                //静态搜索深度不超过普通搜索的1倍
                return SearchQuiesce(alpha, beta, depth);

            MOVE mvBest = new MOVE();
            int best = -G.MATE;
            int vl;

            IEnumerable<MOVE> moves = GetNextMove();
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2} {3}", mv, alpha, beta, best);
                MakeMove(mv);
                depth++;
                vl = -SearchPV(-beta, -alpha, depthleft - 1, out List<MOVE> subpv);
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
                        pvs.Add(mv);
                        pvs.AddRange(subpv);
                    }
                }
            }

            if (mvBest.sqSrc != 0)
                SetBestMove(mvBest);
            return best;
        }

        public int SearchQuiesce(int alpha, int beta, int depthleft)
        {
            stat.QuiesceNodes++;
            int best;
            int sqCheck = stepList[stepList.Count - 1].checking;
            if (sqCheck > 0)
            {
                RepititionResult rep = Repitition();
                if (rep != RepititionResult.NONE)
                    return (int)rep;
                best = depth - G.MATE;
            }
            else
            {
                //对于未被将军的局面，在生成着法前首先对局面作评价；
                int vl = Simple_Evaluate();
                //fail high裁剪。如果超过极限深度，不延伸搜索照将局面，只搜索吃子
                if (vl > beta || depthleft <= 0 && stepList[stepList.Count - 1].move.pcDst == 0)
                    return vl;
                best = vl;
                alpha = Math.Max(alpha, vl);
            }
            IEnumerable<MOVE> moves = GetNextMove();
            foreach (MOVE mv in moves)
            {
                //到了限制层数，只延伸吃子
                if (depthleft <= 0 && mv.pcDst == 0 && sqCheck == 0)
                {
                    continue;
                }
                MakeMove(mv);
                if (depthleft > 0)
                {
                    //未被将军时只延伸照将和吃子的局面
                    if (sqCheck == 0 && mv.pcDst == 0 && stepList[stepList.Count - 1].checking == 0)
                    {
                        UnmakeMove();
                        continue;
                    }
                }
                depth++;
                Debug.Write(new string('\t', depth));
                Debug.WriteLine("{0} {1} {2} {3}", mv, alpha, beta, best);
                int vl = -SearchQuiesce(-beta, -alpha, depthleft - 1);
                depth--;
                UnmakeMove();
                //Debug.Write(new string('\t', depth));
                //Debug.WriteLine("{0} {1}", mv, best);
                if (vl > best)
                {
                    if (vl > beta)
                    {
                        stat.Cutoffs++;
                        //吃送吃的子不记录为推荐着法
                        if (mv.pcDst != stepList[stepList.Count - 1].move.pcSrc)
                            SetBestMove(mv);
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
