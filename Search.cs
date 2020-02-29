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
        const int FUTILITY_MARGIN = 50;

        public STATISTIC stat;
        public List<MOVE> PVLine;
        public List<MOVE> rootMoves;
        TransipositionTable TT;

        internal Stopwatch stopwatch;

        public void InitSearch()
        {
            TT = new TransipositionTable(128);
            stopwatch = new Stopwatch();
            rootMoves = new List<MOVE>();
        }

        public MOVE SearchMain(int maxDepth)
        {
            Debug.Assert(maxDepth > 0);
            stat = new STATISTIC();
            PVLine = new List<MOVE>();
            Array.Clear(MateKiller, 0, G.MAX_PLY);
            Array.Clear(Killers, 0, G.MAX_PLY * 2);
            Array.Clear(History, 0, 14 * 256);
            Array.Clear(HistHit, 0, 14 * 256);
            Array.Clear(HistTotal, 0, 14 * 256);
            TT.Reset();
            rootMoves = new List<MOVE>(GetNextMove(7, 0));

            int vl = 0;

            // 6. 做迭代加深搜索
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                Debug.WriteLine("---------------------------");
                Console.WriteLine("info depth {0}", depth);

                stopwatch.Start();
                vl = SearchRoot(depth);
                stopwatch.Stop();

                stat.ElapsedTime += stopwatch.ElapsedMilliseconds;
                Debug.WriteLine(stat);
                PopPVLine();
                if (rootMoves.Count == 1)
                {
                    Console.WriteLine("Single feasible move");
                    break;
                }

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


        public int SearchRoot(int depth)
        {
            int alpha = -G.MATE;
            int beta = G.MATE;
            MOVE mvBest = new MOVE();
            List<MOVE> subpv = null;
            for (int i = 0; i < rootMoves.Count; i++)
            {
                MOVE mv = rootMoves[i];
                //Console.WriteLine($"info currmove {mv}, currmovenumber {i}");
                Debug.WriteLine($"{mv} {alpha}, {beta}");
                MakeMove(mv, false);
                int vl;

                if (mvBest.sqSrc == 0)
                    vl = -SearchPV(-beta, -alpha, depth - 1, 1, out subpv);
                else
                {
                    vl = -SearchCut(-alpha, depth - 1, 1);
                    if (vl > alpha)
                    {
                        stat.PVChanged++;
                        Debug.WriteLine("Root re-search");
                        Debug.WriteLine($"{mv} {alpha}, {beta}");
                        vl = -SearchPV(-beta, -alpha, depth - 1, 1, out subpv);
                    }
                }
                UnmakeMove();

                mv.score = vl;

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
            rootMoves.RemoveAll(x => x.score < -G.WIN);
            rootMoves.Sort(Large2Small);

            //late move reduction
            for (int i = rootMoves.Count - 1; alpha - rootMoves[i].score > FUTILITY_MARGIN; i--)
            {
                //lose a cannon/knight for nothing
                Console.WriteLine($"Prune move: {rootMoves[i]}, score {rootMoves[i].score}");
                rootMoves.RemoveAt(i);
            }
            Console.WriteLine("Root move\tScore");
            foreach (MOVE mv in rootMoves)
                Console.WriteLine($"{mv}\t{mv.score}");
            Console.WriteLine($"Best move {mvBest}, score {alpha}");
            Console.WriteLine("PV:" + PopPVLine());
            TT.PrintStatus();
            return alpha;
        }

        int HarmlessPruning(int height)
        {
            if (HalfMoveClock >= 120)
                return 0;
            RepititionResult rep = Repitition();
            if (rep != RepititionResult.NONE)
                return (int)rep;

            return -G.MATE + height;
        }

        int SearchPV(int alpha, int beta, int depth, int height, out List<MOVE> pvs)
        {
            pvs = new List<MOVE>();
            if (depth <= 0)
            {
                if (stepList[stepList.Count - 1].move.checking)
                    //被照将时，推迟进入静态搜索
                    depth++;
                else
                    return SearchQuiesce(alpha, beta, 0, height);
            }

            stat.PVNodes++;
            int best = HarmlessPruning(height);
            //distance pruning
            if (best >= beta)
                return best;
            MOVE mvBest = new MOVE();
            bool bResearch = false;
            int hashFlag = 0;
            List<MOVE> subpv = null;
            List<MOVE> played = new List<MOVE>();
            TransKiller = null;
            IEnumerable<MOVE> moves = GetNextMove(7, height);
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', height));
                Debug.WriteLine($"{mv} {alpha}, {beta}, {best}");
                int new_depth = depth - 1;
                if (mv.sqDst == stepList[stepList.Count - 1].move.sqDst && mv.score > 0
                    || mv.checking)
                    new_depth++;
                MakeMove(mv, false);
                int vl;
                if (mvBest.sqSrc == 0)
                    vl = -SearchPV(-beta, -alpha, new_depth, height + 1, out subpv);
                else
                {
                    vl = -SearchCut(-alpha, new_depth, height + 1);
                    if (vl > alpha && vl < beta)
                    {
                        Debug.WriteLine("Re-search");
                        Debug.Write(new string('\t', height));
                        Debug.WriteLine($"{mv} {alpha}, {beta}, {best}");
                        vl = -SearchPV(-beta, -alpha, new_depth, height + 1, out subpv);
                        stat.PVChanged++;
                        bResearch = true;
                    }
                }
                UnmakeMove();
                played.Add(mv);
                if (vl > best)
                {
                    best = vl;
                    if (vl > beta)
                    {
                        stat.Cutoffs++;
                        mvBest = mv;
                        hashFlag = G.HASH_BETA;
                        if (mv.sqDst == 0)
                        {
                            played.Remove(mv);
                            foreach (MOVE m in played)
                                HistoryBad(m);
                            HistoryGood(mv);
                        }
                        break;
                    }
                    if (vl > alpha)
                    {
                        alpha = vl;
                        mvBest = mv;
                        hashFlag = G.HASH_PV;
                        pvs.Clear();
                        pvs.Add(mv);
                        pvs.AddRange(subpv);
                    }
                }
            }
            if (G.UseHash && best > -G.WIN)
            {
                TT.WriteHash(Key, hashFlag, best, depth, mvBest);
                SetBestMove(mvBest, best, depth, height);
            }

            return best;
        }

        int SearchCut(int beta, int depth, int height)
        {
            if (depth <= 0)
            {
                if (stepList[stepList.Count - 1].move.checking)
                    //被照将时，推迟进入静态搜索
                    depth++;
                else
                    return SearchQuiesce(beta - 1, beta, 0, height);
            }

            stat.CutNodes++;
            int best = HarmlessPruning(height);
            if (best > -G.WIN)
                return best;

            if (G.UseHash)
            {
                HashStruct t = TT.ReadHash(Key);
                if (t.Move.sqSrc == 0)
                    TransKiller = null;
                else
                {
                    if (t.AlphaDepth >= depth)
                    {
                        if (t.Alpha < beta)
                            return t.Alpha;
                    }
                    if (t.BetaDepth >= depth)
                    {
                        if (t.Beta >= beta)
                            return t.Beta;
                    }
                    TransKiller = t.Move;
                    TransKiller.pcSrc = pcSquares[TransKiller.sqSrc];
                    TransKiller.pcDst = pcSquares[TransKiller.sqDst];
                }
            }

            IEnumerable<MOVE> moves = GetNextMove(7, height);
            MOVE mvBest = new MOVE();
            List<MOVE> played = new List<MOVE>();
            int opt_value = G.MATE;
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', height));
                Debug.WriteLine($"{mv} {beta - 1}, {beta}, {best}");
                int new_depth = depth - 1;
                if (G.UseFutilityPruning && depth == 1)
                {
                    if (!stepList[stepList.Count - 1].move.checking && new_depth == 0 && mv.pcSrc == 0)
                    {
                        if (opt_value==G.MATE)
                            opt_value = Simple_Evaluate() + G.FutilityMargin;
                        if (opt_value < beta)
                            continue;
                    }
                }

                MakeMove(mv, false);
                int vl = -SearchCut(1 - beta, new_depth, height + 1);
                UnmakeMove();
                played.Add(mv);

                if (vl > best)
                {
                    best = vl;
                    mvBest = mv;
                    if (vl > beta)
                    {
                        if (G.UseHash)
                            TT.WriteHash(Key, G.HASH_BETA, best, depth, mvBest);
                        if (mv.sqDst == 0)
                        {
                            played.Remove(mv);
                            foreach (MOVE m in played)
                                HistoryBad(m);
                            HistoryGood(mv);
                        }
                        stat.Cutoffs++;
                        return vl;
                    }
                }
            }
            if (G.UseHash && best > -G.WIN)
            {
                TT.WriteHash(Key, G.HASH_ALPHA, best, depth, mvBest);
                SetBestMove(mvBest, best, depth, height);
            }
            return best;
        }

        public int SearchQuiesce(int alpha, int beta, int qdepth, int height)
        {
            stat.QuiesceNodes++;
            bool isChecked = stepList[stepList.Count - 1].move.checking;
            int best;
            IEnumerable<MOVE> moves;
            //偶数层是主动方，奇数层是被动方
            TransKiller = null;
            if (qdepth % 2 == 0)
            {
                best = Simple_Evaluate();
                if (best > beta)
                {
                    stat.Cutoffs++;
                    return best;
                }
                if (best > alpha)
                    alpha = best;
                if (isChecked)
                    moves = GetNextMove(7, height);
                else
                {
                    moves = GetNextMove(3, height);
                    ////only extend check and capture
                    //bool continuousCheck = stepList.Count >= 2 && stepList[stepList.Count - 2].move.checking;
                    ////check extension only when in continuous check
                    //if (continuousCheck)
                    //    moves = GetNextMove(3, height);
                    ////capture extension only when recapture
                    //else if (stepList[stepList.Count - 1].move.pcDst > 0)
                    //    moves = GetNextMove(2, height);
                    //else
                    //    return best;
                }
            }
            else
            {
                if (isChecked)    //避免长照
                {
                    RepititionResult rep = Repitition();
                    if (rep != RepititionResult.NONE)
                        return (int)rep;
                    best = height - G.MATE;
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
                if (isChecked)
                    moves = GetNextMove(7, height);
                else
                    moves = GetNextMove(2, height);
            }
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', height));
                Debug.WriteLine($"{mv} {alpha}, {beta}, {best}, {height}");
                MakeMove(mv, false);
                if (qdepth % 2 == 0)
                {
                    if (stepList[stepList.Count - 1].move.checking)
                        stat.CheckExtesions++;
                    else
                        stat.CaptureExtensions++;
                }
                int vl = -SearchQuiesce(-beta, -alpha, qdepth + 1, height + 1);
                UnmakeMove();
                if (vl > beta)
                {
                    stat.Cutoffs++;
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

    }
}
