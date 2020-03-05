﻿#define NULL_MOVE
#define FUTILITY_PRUNING
#undef NULL_VERIFICATION
#undef LATE_MOVE_REDUCTION

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
        List<MOVE> PVLine;
        public List<MOVE> rootMoves;
        TransipositionTable TT;

        internal Stopwatch stopwatch;
        int RootDepth;

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
            Array.Clear(History, 0, 14 * 90);
            Array.Clear(HistHit, 0, 14 * 90);
            Array.Clear(HistTotal, 0, 14 * 90);
            TT.Reset();
            TransKiller = null;
            rootMoves = new List<MOVE>(GetNextMove(7, 0));


            int vl = 0;
            // 6. 做迭代加深搜索
            for (RootDepth = 1; RootDepth <= maxDepth; RootDepth++)
            {
                Debug.WriteLine("---------------------------");
                Console.WriteLine("info depth {0}", RootDepth);

                stopwatch.Start();
                vl = SearchRoot(RootDepth);
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
                if (Math.Abs(vl) > G.WIN)
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
                    if (vl >= beta)
                    {
                        stat.Cutoffs++;
                        break;
                    }
                }
            }
            rootMoves.RemoveAll(x => x.score < -G.WIN);
            rootMoves.Sort(Large2Small);

#if LATE_MOVE_REDUCTION
            //late move reduction
            for (int i = rootMoves.Count - 1; alpha - rootMoves[i].score > FUTILITY_MARGIN; i--)
            {
                //lose a cannon/knight for nothing
                Console.WriteLine($"Prune move: {rootMoves[i]}, score {rootMoves[i].score}");
                rootMoves.RemoveAt(i);
            }
#endif
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

            return -G.MATE + height - 1;
        }

        int SearchPV(int alpha, int beta, int depth, int height, out List<MOVE> pvs)
        {
            beta = Math.Min(beta, G.MATE - height);
            pvs = new List<MOVE>();
            if (depth <= 0)
            {
                if (stepList[stepList.Count - 1].move.checking)
                    //被照将时，推迟进入静态搜索
                    depth++;
                else
                    return SearchQuiesce(alpha, beta, 0, height, RootDepth);
            }

            stat.PVNodes++;
            int best = HarmlessPruning(height);
            //distance pruning
            if (best >= beta)
                return best;
            MOVE mvBest = new MOVE();
            int hashFlag = 0;
            List<MOVE> subpv = null;
            TransKiller = null;
            IEnumerable<MOVE> moves = GetNextMove(7, height);
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', height));
                Debug.WriteLine($"{mv} {alpha}, {beta}, {best},{mv.PrintKiller()}");
                int new_depth = depth - 1;
                if (mv.sqDst == stepList[stepList.Count - 1].move.sqDst && mv.score > 0 || mv.checking)
                    new_depth++;
                MakeMove(mv, false);
                int vl;
                if (mvBest.sqSrc == 0)
                    vl = -SearchPV(Math.Max(-beta, height + 2 - G.MATE), Math.Min(-alpha, G.MATE - height - 2), new_depth, height + 1, out subpv);
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
                    }
                }
                UnmakeMove();
                if (vl > best)
                {
                    best = vl;
                    if (vl >= beta)
                    {
                        stat.Cutoffs++;
                        mvBest = mv;
                        hashFlag = G.HASH_BETA;
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
                else
                    HistoryBad(mv);
            }
            if (best > -G.WIN)
            {
#if USE_HASH
                TT.WriteHash(Key, hashFlag, best, depth, mvBest);
#endif
                if (mvBest.pcSrc != 0)
                    SetBestMove(mvBest, best, depth, height);
            }
            return best;
        }

        int SearchCut(int beta, int depth, int height, bool allowNull = true)
        {
            beta = Math.Min(beta, G.MATE - height);
            if (depth <= 0)
            {
                if (stepList[stepList.Count - 1].move.checking)
                    //被照将时，推迟进入静态搜索
                    depth++;
                else
                    return SearchQuiesce(beta - 1, beta, 0, height, RootDepth);
            }

            stat.CutNodes++;
            int best = HarmlessPruning(height);
            if (best > -G.WIN)
                return best;

#if USE_HASH
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
#endif
#if NULL_MOVE
            if (allowNull && depth >= G.NullDepth)
            {
                MOVE lastMove = stepList[stepList.Count - 1].move;
                //for any opponent pure loss and bad capture, it's better to take it and see the accurate score
                //for any opponent good capture, we are already so bad, it makes no sense to do null move
                //for those equal material exchange, we must finish recapture
                //for mating and checking moves, we can not skip the moves
                if (!lastMove.checking && lastMove.score > HistoryScore && lastMove.score<GoodScore && lastMove.pcDst==0 && Math.Abs(beta) < G.WIN)
                {
                    MakeNullMove();
                    int vl = -SearchCut(1 - beta, depth - G.NullDepth - 1, height + 1, false);
                    UnmakeNullMove();
#if NULL_VERIFICATION
                    if (depth > G.VerReduction && vl >= beta)
                        vl = SearchCut(beta, depth - G.VerReduction, height + 1, false);
#endif
                    if (vl >= beta)
                    {
                        TT.WriteHash(Key, G.HASH_BETA, vl, depth, new MOVE());
                        return vl;
                    }
                }
            }
#endif
            IEnumerable<MOVE> moves = GetNextMove(7, height);
            MOVE mvBest = new MOVE();
            int opt_value = G.MATE;
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', height));
                Debug.WriteLine($"{mv} {beta - 1}, {beta}, {best} {mv.PrintKiller()}");
                int new_depth = depth - 1;
#if FUTILITY_PRUNING
                if (depth == 1)
                {
                    if (!stepList[stepList.Count - 1].move.checking && new_depth == 0 && mv.pcSrc == 0)
                    {
                        if (opt_value == G.MATE)
                            opt_value = Simple_Evaluate() + G.FutilityMargin;
                        if (opt_value < beta)
                            continue;
                    }
                }
#endif
                MakeMove(mv, false);
                int vl = -SearchCut(1 - beta, new_depth, height + 1);
                UnmakeMove();

                if (vl > best)
                {
                    best = vl;
                    mvBest = mv;
                    if (vl >= beta)
                    {
                        SetBestMove(mvBest, best, depth, height);
#if USE_HASH
                        TT.WriteHash(Key, G.HASH_BETA, best, depth, mvBest);
#endif
                        stat.Cutoffs++;
                        return vl;
                    }
                }
                else
                    HistoryBad(mv);
            }
#if USE_HASH
            TT.WriteHash(Key, G.HASH_ALPHA, best, depth, mvBest);
#endif
            return best;
        }

        public int SearchQuiesce(int alpha, int beta, int qheight, int height, int depth)
        {
            beta = Math.Min(beta, G.WIN);
            stat.QuiesceNodes++;
            bool isChecked = stepList[stepList.Count - 1].move.checking;
            if (qheight > G.MAX_QUEISCE_DEPTH)
                return Simple_Evaluate();
            int best;
            IEnumerable<MOVE> moves;
            //偶数层是主动方，奇数层是被动方
            TransKiller = null;
            if (qheight % 2 == 0)
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
                }
            }
            else
            {
                if (isChecked)    //避免长照
                {
                    RepititionResult rep = Repitition();
                    if (rep != RepititionResult.NONE)
                        return (int)rep;
                    best = height - G.MATE - 1;
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
            bool first_move = true;
            int vl;
            foreach (MOVE mv in moves)
            {
                if (!isChecked && depth <= 0 && mv.pcDst == 0)
                    continue;
                Debug.Write(new string('\t', height));
                Debug.WriteLine($"{mv} {alpha}, {beta}, {best}, {height}");
                MakeMove(mv, false);
                if (qheight % 2 == 0)
                {
                    if (stepList[stepList.Count - 1].move.checking)
                        stat.CheckExtesions++;
                    else
                        stat.CaptureExtensions++;
                }
                if (first_move)
                {
                    vl = -SearchQuiesce(-beta, -alpha, qheight + 1, height + 1, depth);
                    first_move = false;
                }
                else
                    vl = -SearchQuiesce(-beta, -alpha, qheight + 1, height + 1, depth - 1);
                UnmakeMove();
                if (vl >= beta)
                {
                    stat.Cutoffs++;
                    SetBestMove(mv, vl, 0, height);
                    return vl;
                }
                if (vl >= best)
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
