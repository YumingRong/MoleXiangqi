﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    partial class POSITION
    {
        internal MOVE[,] Killers = new MOVE[G.MAX_PLY, 2];
        internal int[,] History = new int[14, 256]; //there are 7 kinds of pieces in each side
        internal int[,] HistTotal = new int[14, 256];
        internal int[,] HistHit = new int[14, 256];
        internal MOVE[] MateKiller;

        const int HistoryMax = 0x4000;

        const int TransScore = +32766;
        const int GoodScore = +4000;
        const int KillerScore = +4;
        const int HistoryScore = -24000;
        const int BadScore = -28000;

        public readonly static int[] cnPieceHistIndex = {
          -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
          0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 5, 5, 6, 6,
          7, 8, 8, 9, 9,10,10,11,11,11,11,11,12,12,13,13
        };

        void SetBestMove(MOVE mv, int score, int depthleft)
        {
            Debug.Assert(mv.sqSrc != 0);
            Debug.Assert(score > -G.MATE && score < G.MATE);
            Debug.Assert(depthleft >= 0);
            if (mv.pcDst > 0)
                return;
            if (Killers[depth, 0] != mv)
            {
                Killers[depth, 1] = Killers[depth, 0];
                Killers[depth, 0] = mv;
            }
            History[cnPieceTypes[mv.pcSrc] - 17, mv.sqDst] += depthleft * depthleft;
            if (History[cnPieceTypes[mv.pcSrc] - 17, mv.sqDst] > HistoryMax)
            {
                for (int pc = 0; pc < 14; pc++)
                    for (int x = FILE_LEFT; x <= FILE_RIGHT; x++)
                        for (int y = RANK_TOP; y <= RANK_BOTTOM; y++)
                        {
                            int sq = XY2Coord(x, y);
                            History[pc, sq] /= 2;
                        }
            }
            if (score > G.WIN)
                MateKiller[depth] = mv;
        }

        void HistoryGood(MOVE mv)
        {
            if (mv.pcDst > 0)
                return;
            HistHit[cnPieceHistIndex[mv.pcSrc], mv.sqDst]++;
            HistTotal[cnPieceHistIndex[mv.pcSrc], mv.sqDst]++;
        }

        void HistoryBad(MOVE mv)
        {
            if (mv.pcDst > 0)
                return;
            HistTotal[cnPieceHistIndex[mv.pcSrc], mv.sqDst]++;
        }

        /*该函数首先返回matekiller，然后返回killer move
         * 对于其它着法，按吃子、照将、移动的优先级打分
         * 参数onlyCheckCapture: 
         * 1 - 生成所有照将
         * 2 - 生成所有吃子，不包括吃仕相和未过河的兵
         * 3 - 生成所有照将和吃子
         * 7 - 生成所有合法着法
        */
        public IEnumerable<MOVE> GetNextMove(int moveType)
        {
            bool wantCheck = (moveType & 0x01) > 0;
            bool wantCapture = (moveType & 0x02) > 0;
            bool wantAll = moveType == 7;

            MOVE killer = MateKiller[depth];
            if (killer.pcSrc == pcSquares[killer.sqSrc] && killer.pcDst == pcSquares[killer.sqDst]
                && IsLegalMove(killer.sqSrc, killer.sqDst))
            {
                MovePiece(killer);
                bool notChecked = (CheckedBy(1 - sdPlayer) == 0);
                UndoMovePiece(killer);
                if (notChecked)
                    if (wantAll || wantCheck && IsChecking(killer) || wantCapture && killer.pcDst > 0)
                    {
                        yield return killer;
                    }
            }

            List<MOVE> moves = GenerateMoves();
            GenAttackMap();
            int[] scores = new int[moves.Count];
            int[] kinds = new int[moves.Count];//check, capture or normal move

            //assign killer bonus
            if (Killers[sdPlayer, 0].sqSrc > 0)
            {
                int j = moves.FindIndex(x => x == Killers[sdPlayer, 0]);
                if (j > -1)
                    scores[j] = 15;
                if (Killers[sdPlayer, 0].sqSrc > 0)
                {
                    j = moves.FindIndex(x => x == Killers[sdPlayer, 1]);
                    if (j > -1)
                        scores[j] = 10;
                }
            }
            for (int i = 0; i < moves.Count; i++)
            {
                MOVE mv = moves[i];
                if (wantCheck && IsChecking(mv))
                {
                    kinds[i] |= 1;
                    //encourage continuous check
                    if (stepList.Count >= 2 && stepList[stepList.Count - 2].checking > 0)
                        scores[i] += 30;
                    else
                        scores[i] += 10;
                    //Avoid repeating check by the same piece 
                    if (mv.pcDst == 0 && stepList.Count >= 2 && mv.sqSrc == stepList[stepList.Count - 2].checking)
                        scores[i] -= 5;
                }
                else if (mv.pcDst > 0 && !(cnPieceKinds[mv.pcDst] >= 5 && HOME_HALF[SIDE(mv.pcDst), mv.sqDst]))
                {//吃子，不包括吃仕相和未过河的兵
                    kinds[i] |= 2;
                }
                else if (wantAll)
                {
                    //scores[i] += HistHit[cnPieceHistIndex[mv.pcSrc], mv.sqDst] * HistoryMax / (HistTotal[cnPieceHistIndex[mv.pcSrc], mv.sqDst] + 1) + HistoryScore;
                    kinds[i] |= 4;
                }
                scores[i] += SEE(mv, attackMap[sdPlayer, mv.sqDst], attackMap[1 - sdPlayer, mv.sqDst]);
            }
            List<KeyValuePair<MOVE, int>> l = new List<KeyValuePair<MOVE, int>>();
            for (int i = 0; i < moves.Count; i++)
            {
                if ((kinds[i] & moveType) != 0)
                {
                    KeyValuePair<MOVE, int> t = new KeyValuePair<MOVE, int>(moves[i], scores[i]);
                    l.Add(t);
                }
            }
            l.Sort(SortLarge2Small);
            foreach (var t in l)
                yield return t.Key;
        }

        public List<KeyValuePair<MOVE, int>> InitRootMoves()
        {
            List<MOVE> moves = GenerateMoves();
            GenAttackMap();
            int[] scores = new int[moves.Count];
            for (int i = 0; i < moves.Count; i++)
            {
                MOVE mv = moves[i];
                if (IsChecking(mv))
                {
                    //encourage continuous check
                    if (stepList.Count >= 2 && stepList[stepList.Count - 2].checking > 0)
                        scores[i] += 30;
                    else
                        scores[i] += 10;
                    //Avoid repeating check by the same piece 
                    if (mv.pcDst == 0 && stepList.Count >= 2 && mv.sqSrc == stepList[stepList.Count - 2].checking)
                        scores[i] -= 5;
                }
                scores[i] += SEE(mv, attackMap[sdPlayer, mv.sqDst], attackMap[1 - sdPlayer, mv.sqDst]);
            }
            List<KeyValuePair<MOVE, int>> l = new List<KeyValuePair<MOVE, int>>();
            for (int i = 0; i < moves.Count; i++)
            {
                KeyValuePair<MOVE, int> t = new KeyValuePair<MOVE, int>(moves[i], scores[i]);
                l.Add(t);
            }
            l.Sort(SortLarge2Small);
            return l;
        }

        bool IsChecking(MOVE m)
        {
            MovePiece(m);
            int sqchecking = CheckedBy(sdPlayer);
            UndoMovePiece(m);
            return sqchecking > 0;
        }

        public void GenMoveTest()
        {
            foreach (var mv in InitRootMoves())
                Console.WriteLine(mv.Key);
            Console.WriteLine("End of moves");
        }

        //SEE is basically as same as SubSEE. But it copy the attack and defend list to make them mutable
        //SEE can return negative value. SubSEE only return positive value. 
        int SEE(MOVE mv, in List<int> attackers, List<int> defenders)
        {
            Debug.Assert(mv.sqSrc != 0);
            Debug.Assert(attackers != null);
            Debug.Assert(defenders != null);
            if (!(cnPieceKinds[mv.pcSrc] == CANNON && mv.pcDst == 0))
                Debug.Assert(attackers.Contains(mv.pcSrc));

            List<int> atts = new List<int>(attackers);
            List<int> defs = new List<int>(defenders);

            int sqDst = mv.sqDst;
            int vlDst = cnPieceValue[pcSquares[sqDst]];
            MovePiece(mv);
            Debug.Assert(CheckedBy(1 - sdPlayer) == 0);
            //find defending slider piece behind the attacking piece
            int attKind = cnPieceKinds[mv.pcSrc];
            bool potentialHideSlider = attKind == PAWN || attKind == ROOK || attKind == KING;
            if (potentialHideSlider)
            {
                foreach (int pc in attackMap[sdPlayer, mv.sqSrc])
                {
                    int d = cnPieceKinds[pc];
                    if (d == CANNON || d == ROOK)
                    {
                        int sqDef = sqPieces[pc];
                        if ((SAME_FILE(sqDef, sqDst) || SAME_RANK(sqDef, sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sqDef))
                        {
                            defs.Add(pc);
                            defs.Sort(delegate (int x, int y) { return y.CompareTo(x); });
                        }
                    }
                }
                //cancel defending cannon behind the piece
                if (defs.Count > 0 && cnPieceKinds[defs[0]] == CANNON)
                {
                    int sqCannon = sqPieces[defs[0]];
                    if ((SAME_FILE(sqCannon, mv.sqSrc) || SAME_RANK(sqCannon, mv.sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sqCannon))
                        defs.RemoveAt(0);
                }
            }
            if (defs.Count == 0)
            {
                UndoMovePiece(mv);
                return vlDst;
            }
            if (!(cnPieceKinds[mv.pcSrc] == CANNON && mv.pcDst == 0)) //only for tier 1
                atts.Remove(mv.pcSrc);
            if (potentialHideSlider)
            {
                foreach (int pc in attackMap[SIDE(mv.pcSrc), mv.sqSrc])
                {
                    int d = cnPieceKinds[pc];
                    if (d == CANNON || d == ROOK)
                    {
                        int sqDef = sqPieces[pc];
                        if ((SAME_FILE(sqDef, sqDst) || SAME_RANK(sqDef, sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sqDef))
                        {
                            atts.Add(pc);
                            atts.Sort(delegate (int x, int y) { return y.CompareTo(x); });
                        }
                    }
                }
                //cancel attacking cannon behind the piece
                if (atts.Count > 0 && cnPieceKinds[atts[0]] == CANNON)
                {
                    int sqCannon = sqPieces[atts[0]];
                    if ((SAME_FILE(sqCannon, mv.sqSrc) || SAME_RANK(sqCannon, mv.sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sqCannon))
                        atts.RemoveAt(0);
                }
            }
            MOVE dmv = new MOVE(sqPieces[defs[0]], sqDst, defs[0], mv.pcSrc);
            Debug.Assert(IsLegalMove(sqPieces[defs[0]], sqDst));
            int score = -SubSEE(dmv, defs, atts);
            UndoMovePiece(mv);
            return vlDst + score;
        }

        int SubSEE(MOVE mv, in List<int> atts, List<int> defs)
        {
            Debug.Assert(mv.sqSrc != 0);
            Debug.Assert(atts != null);
            Debug.Assert(defs != null);
            if (!(cnPieceKinds[mv.pcSrc] == CANNON && mv.pcDst == 0))
                Debug.Assert(atts.Contains(mv.pcSrc));

            int sqDst = mv.sqDst;
            int vlDst = cnPieceValue[pcSquares[sqDst]];
            MovePiece(mv);
            if (cnPieceKinds[mv.pcSrc] == KING)
                if (defs.Count > 0 || KingsFace2Face())
                {
                    UndoMovePiece(mv);
                    return 0;
                }
            //find defending slider piece behind the attacking piece
            int attKind = cnPieceKinds[mv.pcSrc];
            bool potentialHideSlider = attKind == PAWN || attKind == ROOK || attKind == KING;
            if (potentialHideSlider)
            {
                foreach (int pc in attackMap[sdPlayer, mv.sqSrc])
                {
                    int d = cnPieceKinds[pc];
                    if (d == CANNON || d == ROOK)
                    {
                        int sqDef = sqPieces[pc];
                        if ((SAME_FILE(sqDef, sqDst) || SAME_RANK(sqDef, sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sqDef))
                        {
                            defs.Add(pc);
                            defs.Sort(delegate (int x, int y) { return y.CompareTo(x); });
                        }
                    }
                }
                //cancel defending cannon behind the piece
                if (defs.Count > 0 && cnPieceKinds[defs[0]] == CANNON)
                {
                    int sqCannon = sqPieces[defs[0]];
                    if ((SAME_FILE(sqCannon, mv.sqSrc) || SAME_RANK(sqCannon, mv.sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sqCannon))
                        defs.RemoveAt(0);
                }
            }
            if (defs.Count == 0)
            {
                UndoMovePiece(mv);
                return vlDst;
            }
            atts.Remove(mv.pcSrc);
            if (potentialHideSlider)
            {
                foreach (int pc in attackMap[SIDE(mv.pcSrc), mv.sqSrc])
                {
                    int d = cnPieceKinds[pc];
                    if (d == CANNON || d == ROOK)
                    {
                        int sqDef = sqPieces[pc];
                        if ((SAME_FILE(sqDef, sqDst) || SAME_RANK(sqDef, sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sqDef))
                        {
                            atts.Add(pc);
                            atts.Sort(delegate (int x, int y) { return y.CompareTo(x); });
                        }
                    }
                }
                //cancel attacking cannon behind the piece
                if (atts.Count > 0 && cnPieceKinds[atts[0]] == CANNON)
                {
                    int sqCannon = sqPieces[atts[0]];
                    if ((SAME_FILE(sqCannon, mv.sqSrc) || SAME_RANK(sqCannon, mv.sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sqCannon))
                        atts.RemoveAt(0);
                }
            }
            MOVE dmv = new MOVE(sqPieces[defs[0]], sqDst, defs[0], mv.pcSrc);
            Debug.Assert(IsLegalMove(sqPieces[defs[0]], sqDst));
            int score = -SubSEE(dmv, defs, atts);
            UndoMovePiece(mv);
            return Math.Max(0, vlDst + score);
        }

    }
}
