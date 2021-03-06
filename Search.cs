﻿#define NULL_MOVE
#define FUTILITY_PRUNING
#undef NULL_VERIFICATION
#undef LATE_MOVE_REDUCTION
#define INTERNAL_ITERATIVE_DEEPENING 
#undef USE_MATEKILLER
#define HISTORY_PRUNING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MoleXiangqi
{
    public struct STATISTIC
    {
        public int QuiesceNodes, PVNodes, ZeroWindowNodes;
        public long ElapsedTime;
        public int BetaCutoffs, NullCutoffs, PVChanged, HistoryResearched, HistoryReduced;
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
            int totalNodes = QuiesceNodes + PVNodes + ZeroWindowNodes;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Nodes: total {totalNodes}， PV {PVNodes}, Cut {ZeroWindowNodes}, Quiesce {QuiesceNodes}");
            sb.AppendLine($"Elapsed time: {ElapsedTime} millisecond. Nodes per second: {totalNodes * 1000 / ElapsedTime}");
            sb.AppendLine($"BetaCutoffs: {BetaCutoffs}, NullCutoffs: {NullCutoffs}, PV re-searched: {PVChanged}");
            sb.AppendLine($"History: reduced {HistoryReduced}, researched {HistoryResearched}");
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
        OpeningBook Book;
        internal Stopwatch stopwatch;
        int RootDepth;

        void InitSearch()
        {
            TT = new TransipositionTable(128);
            stopwatch = new Stopwatch();
            rootMoves = new List<MOVE>();
            Book = new OpeningBook();
        }

        public MOVE SearchMain(int maxDepth)
        {
            Debug.Assert(maxDepth > 0);

            Book.ReadBook(@"J:\C#\MoleXiangqi\Book.dat");
            MOVE book_move = SearchOpeningBook();
            if (book_move != null)
                return book_move;

            stat = new STATISTIC();
            PVLine = new List<MOVE>();
#if USE_MATEKILLER
            for (int i = 0; i < G.MAX_PLY; i++)
                MateKiller[i] = new MOVE();
#endif
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
            ProbeHistory();
            return PVLine[0];
        }

        public int SearchRoot(int depth)
        {
            int alpha = -G.MATE;
            int beta = G.MATE;
            MOVE mvBest = new MOVE();
            List<MOVE> subpv = null;
            bool firstMove = true;
            for (int i = 0; i < rootMoves.Count; i++)
            {
                MOVE mv = rootMoves[i];
                //Console.WriteLine($"info currmove {mv}, currmovenumber {i}");
                Debug.WriteLine($"{mv} {alpha}, {beta}");
                int new_depth = depth - 1;
                if (mv.sqDst == stepList[stepList.Count - 1].move.sqDst && mv.score > 0
                    || mv.checking || stepList[stepList.Count - 1].move.checking && firstMove)
                    new_depth++;
                firstMove = false;
                MakeMove(mv, false);
                int vl;

                if (mvBest.sqSrc == 0)
                    vl = -SearchPV(-beta, -alpha, new_depth, 1, out subpv);
                else
                {
                    vl = -SearchCut(-alpha - 1, -alpha, new_depth, 1, -1);
                    if (vl > alpha)
                    {
                        stat.PVChanged++;
                        Debug.WriteLine("Root re-search");
                        Debug.WriteLine($"{mv} {alpha}, {beta}");
                        vl = -SearchPV(-beta, -alpha, new_depth, 1, out subpv);
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
                        stat.BetaCutoffs++;
                        break;
                    }
                }
            }
            rootMoves.RemoveAll(x => x.score < -G.WIN);
            rootMoves.Sort(Large2Small);

#if LATE_MOVE_REDUCTION
            int vlBase = Simple_Evaluate();
            //late move reduction
            for (int i = rootMoves.Count - 1; i >= 0 && vlBase - rootMoves[i].score > 50; i--)
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
                    return SearchQuiesce(alpha, beta, 0, height);
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
#if USE_HASH
            HashStruct t = TT.ReadHash(Key);
            if (t.Move.sqSrc == 0)
                TransKiller = null;
            else
            {
                if (t.AlphaDepth >= depth)
                {
                    if (t.Alpha <= alpha)
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
#if INTERNAL_ITERATIVE_DEEPENING
            if (depth >= G.IIDDepth && TransKiller is null)
            {
                int new_depth = depth - G.IIDReduction;
                Debug.Assert(new_depth > 0);
                int vl = SearchPV(alpha, beta, new_depth, height, out subpv);
                if (vl < alpha)
                    vl = SearchPV(-G.MATE, beta, new_depth, height, out subpv);
                if (subpv.Count > 0)
                    TransKiller = subpv[0];
            }
#endif

            IEnumerable<MOVE> moves = GetNextMove(7, height);
            foreach (MOVE mv in moves)
            {
                Debug.Write(new string('\t', height));
                Debug.WriteLine($"{mv} {alpha}, {beta}, {best},{mv.killer}");
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
                    //当depth == 0, 且best < alpha时，传给quiesce的beta用 -best代替 -alpha，可以避免eval() > beta过早返回。
                    //返回的值和最佳着法只是因为搜索深度浅而貌似高于best。后续重新搜索时这个非最佳着法被首先搜索，降低了效率。
                    if (new_depth == 0 && alpha < G.WIN)
                        vl = -SearchCut(-alpha - 1, -best, new_depth, height + 1, -1);
                    else
                        vl = -SearchCut(-alpha - 1, -alpha, new_depth, height + 1, -1);
                    if (vl > alpha) // && vl < beta
                    {
                        Debug.WriteLine("Re-search");
                        Debug.Write(new string('\t', height));
                        Debug.WriteLine($"{mv} {alpha}, {beta}, {vl}");
                        vl = -SearchPV(-beta, -vl, new_depth, height + 1, out subpv);
                        stat.PVChanged++;
                    }
                }
                UnmakeMove();
                if (vl > best)
                {
                    best = vl;
                    if (vl >= beta)
                    {
                        stat.BetaCutoffs++;
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

        int SearchCut(int alpha, int beta, int depth, int height, int mate_threat, bool allowNull = true)
        {
            beta = Math.Min(beta, G.MATE - height);
            if (depth <= 0)
            {
                if (stepList[stepList.Count - 1].move.checking)
                    //被照将时，推迟进入静态搜索
                    depth++;
                else
                    return SearchQuiesce(alpha, beta, 0, height);
            }

            stat.ZeroWindowNodes++;
            int best = HarmlessPruning(height);
            if (best >= beta)
                return best;

#if USE_HASH
            HashStruct t = TT.ReadHash(Key);
            if (t.Move.sqSrc == 0)
                TransKiller = null;
            else
            {
                if (t.AlphaDepth >= depth)
                {
                    if (t.Alpha <= alpha)
                        return t.Alpha;
                }
                if (t.BetaDepth >= depth)
                {
                    if (t.Beta >= beta)
                        return t.Beta;
                }
                if (t.Alpha == t.Beta)
                    return t.Alpha;
                TransKiller = t.Move;
                TransKiller.pcSrc = pcSquares[TransKiller.sqSrc];
                TransKiller.pcDst = pcSquares[TransKiller.sqDst];
            }
#endif
            Debug.Assert(height < G.MAX_PLY);
            MOVE lastMove = stepList[stepList.Count - 1].move;
#if NULL_MOVE
            if (allowNull && depth >= G.NullDepth)
            {
                //for any opponent pure loss and bad capture, it's better to take it and see the accurate score
                //for any opponent good capture, we are already so bad, it makes no sense to do null move
                //for those equal material exchange, we must finish recapture
                //for mating and checking moves, we can not skip the moves
                if (!lastMove.checking && lastMove.score > HistoryScore && lastMove.score < GoodScore && lastMove.pcDst == 0 && Math.Abs(beta) < G.WIN)
                {
                    MakeNullMove();
                    int vl = -SearchCut(-beta, -alpha, depth - G.NullReduction - 1, height + 1, mate_threat, false);
                    UnmakeNullMove();
#if NULL_VERIFICATION
                    if (depth > G.VerReduction && vl >= beta)
                        vl = SearchCut(beta, depth - G.VerReduction, height + 1, false);
#endif
                    if (vl >= beta)
                    {
                        Debug.Assert(vl < G.WIN);   // do not return unproven mates
                        TT.WriteHash(Key, G.HASH_BETA, vl, depth, new MOVE());
                        stat.NullCutoffs++;
                        return vl;
                    }
                }
            }
#endif
            IEnumerable<MOVE> moves = GetNextMove(7, height, mate_threat);
            MOVE mvBest = new MOVE();
            int opt_value = G.MATE;
            List<MOVE> mvPlayed = new List<MOVE>();
            foreach (MOVE mv in moves)
            {
                int new_depth = depth - 1;
                if (mv.checking)
                    new_depth++;
                if (mate_threat < 0)
                {
                    if (best < -G.WIN && mvPlayed.Count > 0)
                    {
                        mate_threat = height;
                        alpha = -G.WIN;
                    }
                }
                else
                {
                    MOVE matekiller = Killers[mate_threat + 1, 0];
                    if (mv == matekiller || mate_threat == height && best > -G.WIN || !IsLegalMove(matekiller.sqSrc, matekiller.sqDst))
                        mate_threat = -1;
                }
#if HISTORY_PRUNING
                bool reduced = false;
                if (depth >= G.HistoryDepth && !lastMove.checking && new_depth < depth && mvPlayed.Count >= G.HistoryMoveNb)
                {
                    int k = GetHistoryIndex(mv);
                    if ((float)(HistHit[k]) / HistTotal[k] < 0.6)
                    {
                        new_depth--;
                        reduced = true;
                        stat.HistoryReduced++;
                    }
                }
#endif
#if FUTILITY_PRUNING
                if (depth == 1 && !lastMove.checking && !mv.checking && new_depth == 0 && mv.pcDst == 0)
                {
                    if (opt_value == G.MATE)
                        opt_value = Simple_Evaluate() + G.FutilityMargin;
                    if (opt_value < beta)
                        continue;
                }
#endif
                //if the opponent is delaying mate by checking, do not enter quiesce until the mate killer move is tried
                if (new_depth == 0 && mate_threat > 0)
                    new_depth = 1;
                Debug.Write(new string('\t', height));
                Debug.WriteLine($"{mv} {alpha}, {beta}, {best} {mv.killer}");
                MakeMove(mv, false);
                int vl;
                //to avoid quiesce search beta cut off too early, use -best instead of -alpha as new beta
                if (new_depth == 0 && alpha < G.WIN)
                    vl = -SearchCut(-beta, -best, 0, height + 1, mate_threat);
                else
                    vl = -SearchCut(-beta, -alpha, new_depth, height + 1, mate_threat);
#if HISTORY_PRUNING
                if (vl >= beta && reduced)
                {
                    new_depth++;
                    vl = -SearchCut(-beta, -alpha, new_depth, height + 1, mate_threat);
                    stat.HistoryResearched++;
                }
#endif
                UnmakeMove();

                if (vl > best)
                {
                    best = vl;
                    mvBest = mv;
                    if (vl >= beta)
                    {
                        SetBestMove(mvBest, best, depth, height);
                        foreach (MOVE m1 in mvPlayed)
                            HistoryBad(m1);
#if USE_HASH
                        TT.WriteHash(Key, G.HASH_BETA, best, depth, mvBest);
#endif
                        stat.BetaCutoffs++;
                        return vl;
                    }
                    if (vl > alpha)
                    {
                        alpha = vl;
                    }
                }
                mvPlayed.Add(mv);
            }
#if USE_HASH
            TT.WriteHash(Key, G.HASH_ALPHA, best, depth, mvBest);
#endif
            return best;
        }

        public int SearchQuiesce(int alpha, int beta, int qheight, int height)
        {
            beta = Math.Min(beta, G.WIN);
            stat.QuiesceNodes++;
            MOVE mvLast = stepList[stepList.Count - 1].move;
            bool isChecked = mvLast.checking;
            int best;
            IEnumerable<MOVE> moves;
            //偶数层是主动方，奇数层是被动方
            TransKiller = null;
            if (qheight % 2 == 0)
            {
                if (isChecked)
                {
                    best = -G.MATE + height - 1;
                    moves = GetNextMove(7, height);
                }
                else
                {
                    best = Simple_Evaluate();
                    moves = GetNextMove(3, height);

                    if (best > beta && mvLast.pcDst == 0 && stepList[stepList.Count - 2].move.pcDst == 0)
                    {
                        stat.BetaCutoffs++;
                        return best;
                    }
                    if (qheight > G.MAX_QUEISCE_DEPTH && mvLast.pcDst == 0)
                        return best;
                    if (best > alpha)
                        alpha = best;
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
                    if (best > beta && mvLast.pcDst == 0 && stepList[stepList.Count - 2].move.pcDst == 0)
                    {
                        stat.BetaCutoffs++;
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
            int vl;
            foreach (MOVE mv in moves)
            {
                //if (!isChecked && height >= RootDepth * 2 && mv.pcDst == 0)
                //    continue;
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
                vl = -SearchQuiesce(-beta, -alpha, qheight + 1, height + 1);
                UnmakeMove();
                if (vl >= beta)
                {
                    stat.BetaCutoffs++;
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

        MOVE SearchOpeningBook()
        {
            List<MOVE> nextmoves = new List<MOVE>();
            int best = 0;
            if (Book.TryGetValue(Key, out BookEntry entry))
            {
                List<MOVE> moves = GenerateMoves();
                int myside = sdPlayer;
                foreach (MOVE mv in moves)
                {
                    MakeMove(mv);
                    if (Book.TryGetValue(Key, out BookEntry entry1))
                    {
                        if (myside == 0)
                            mv.score = (entry1.win * 2 + entry1.draw) * 1000 / (entry1.win + entry1.draw + entry1.loss);
                        else
                            mv.score = (entry1.loss * 2 + entry1.draw) * 1000 / (entry1.win + entry1.draw + entry1.loss);
                        if (mv.score > best)
                        {
                            best = mv.score;
                            nextmoves.Add(mv);
                        }
                    }
                    UnmakeMove();
                }
            }
            else
            {
                //Try mirror. Nearly same code. 
                ulong mirror_key = CalculateZobrist(true);
                if (Book.TryGetValue(mirror_key, out entry))
                {
                    List<MOVE> moves = GenerateMoves();
                    int myside = sdPlayer;
                    foreach (MOVE mv in moves)
                    {
                        MakeMove(mv);
                        mirror_key = CalculateZobrist(true);
                        if (Book.TryGetValue(mirror_key, out BookEntry entry1))
                        {
                            if (myside == 0)
                                mv.score = entry1.win * 1000 / (entry1.win + entry1.draw + entry1.loss);
                            else
                                mv.score = entry1.loss * 1000 / (entry1.win + entry1.draw + entry1.loss);
                            if (mv.score > best)
                            {
                                best = mv.score;
                                nextmoves.Add(mv);
                            }
                        }
                        UnmakeMove();
                    }
                }
                else
                    return null;
            }
            nextmoves.RemoveAll(x => x.score < best * G.OpeningRandom);
            Random rnd = new Random();
            return nextmoves[rnd.Next(0, nextmoves.Count)];
        }
    }
}
