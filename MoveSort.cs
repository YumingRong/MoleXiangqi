﻿#undef USE_MATEKILLER
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    partial class POSITION
    {
        internal MOVE[,] Killers = new MOVE[G.MAX_PLY, 2];
        internal int[] History = new int[14 * 90]; //there are 7 kinds of pieces in each side
        internal int[] HistTotal = new int[14 * 90];
        internal int[] HistHit = new int[14 * 90];
        internal MOVE[] MateKiller = new MOVE[G.MAX_PLY];
        MOVE TransKiller;

        const int HistoryMax = 0x4000;

        const int TransScore = +32766;
        const int GoodScore = +4000;
        const int KillerScore = +4;
        const int HistoryScore = -24000;
        const int BadScore = -28000;

        public readonly static int[] cnPieceHistIndex = {
          -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
          0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 5, 5, 6, 6,
          7, 8, 8, 9, 9,10,10,11,11,11,11,11,12,12,13,13
        };

        static int GetHistoryIndex(MOVE mv)
        {
            return cnPieceHistIndex[mv.pcSrc] * 90 + cboard256[mv.sqDst];
        }

        void SetBestMove(MOVE mv, int score, int depth, int height)
        {
            Debug.Assert(mv.sqSrc != 0);
            Debug.Assert(score > -G.MATE && score < G.MATE);
            Debug.Assert(depth >= 0);

            //吃送吃的子没有必要记录
            if (mv.pcDst > 0 && mv.sqDst == stepList[stepList.Count - 1].move.sqDst)
                return;
#if USE_MATEKILLER
            if (score > G.RULEWIN)
            {
                Debug.Assert(sdPlayer == SIDE(mv.pcSrc));
                MateKiller[height].sqSrc = mv.sqSrc;
                MateKiller[height].sqDst = mv.sqDst;
                MateKiller[height].pcSrc = mv.pcSrc;
                MateKiller[height].pcDst = mv.pcDst;
            }
            else 
#endif
            if (Killers[height, 0] != mv)
            {
                Killers[height, 1] = Killers[height, 0];
                Killers[height, 0] = mv;
            }
            HistoryGood(mv, depth);
        }

        void HistoryGood(MOVE mv, int depth)
        {
            int i = GetHistoryIndex(mv);
            HistHit[i]++;
            HistTotal[i]++;

            History[i] += depth * depth;
        }

        void HistoryBad(MOVE mv)
        {
            int i = GetHistoryIndex(mv);
            HistTotal[i]++;
        }

        void ProbeHistory()
        {
            int[] top = new int[16];
            for (int i = 0; i < 14 * 90; i++)
            {
                int j = top.Length - 1;
                if (GetValue(i) > GetValue(top[j]))
                {
                    while (j > 0 && GetValue(i) > GetValue(top[j - 1]))
                    {
                        top[j] = top[j - 1];
                        j--;
                    }
                    top[j] = i;
                }
            }
            Console.WriteLine("Top history:");
            Console.WriteLine("\tScore\tHistory\tHit\tTotal");
            for (int i = 0; i < top.Length; i++)
            {
                int j = top[i];
                Console.WriteLine($"{Index2String(j)}\t{GetValue(j)}\t{History[j]}\t{HistHit[j]}\t{HistTotal[j]}");
            }

            int GetValue(int i)
            {
                if (HistTotal[i] > 0)
                    return History[i] * HistHit[i] / HistTotal[i];
                else
                    return 0;
            }

            string Index2String(int i)
            {
                int pc = i / 90;
                int sq = i % 90;
                Debug.Assert(pc >= 0 && pc <= 13);
                string s = pc > 6 ? "B" : "R";
                string PC = "KRCNPBG";
                s += PC[pc % 7] + " ";
                s += Coord2ICCS(cboard90[sq]);
                return s;

            }
        }

        /*该函数首先返回Transkiller，然后生成所有着法，过滤不需要的着法后SEE，排序
         * 参数moveType: 
         * 1 - 生成所有照将
         * 2 - 生成所有吃子
         * 3 - 生成所有照将和吃子
         * 7 - 生成所有合法着法
        */
        public IEnumerable<MOVE> GetNextMove(int moveType, int height, int mate_threat = -1)
        {
            Debug.Assert(moveType < 8);
            Debug.Assert(height >= 0);
            bool wantCheck = (moveType & 0x01) > 0;
            bool wantCapture = (moveType & 0x02) > 0;
            bool wantAll = moveType == 7;

            List<MOVE> movesDone = new List<MOVE>();
#if USE_HASH
            if (!(TransKiller is null) && TransKiller.sqDst != 0)
            {
                Debug.Assert(TransKiller.pcSrc == pcSquares[TransKiller.sqSrc]);
                Debug.Assert(TransKiller.pcDst == pcSquares[TransKiller.sqDst]);
                Debug.Assert(IsLegalMove(TransKiller.sqSrc, TransKiller.sqDst));
                TransKiller.killer = KILLER.Trans;
                TransKiller.score = TransScore; //unnecessary
                movesDone.Add(TransKiller);
                yield return TransKiller;
            }
#endif
            MOVE killer;
#if USE_MATEKILLER
            if (!stepList[stepList.Count -1].move.checking)
            {
                killer = MateKiller[sdPlayer];
                if (!(killer is null) && killer.pcSrc == pcSquares[killer.sqSrc] && killer.pcDst == pcSquares[killer.sqDst]
    && IsLegalMove(killer.sqSrc, killer.sqDst))
                {
                    MovePiece(killer);
                    bool notChecked = (CheckedBy(1 - sdPlayer) == 0);
                    killer.checking = CheckedBy(sdPlayer) > 0;
                    UndoMovePiece(killer);
                    if (notChecked)
                        if (wantAll || wantCheck && IsChecking(killer) || wantCapture && killer.pcDst > 0)
                        {
                            movesDone.Add(killer);
                            killer.killer = KILLER.Mate;
                            yield return killer;
                        }
                }
            }
#endif
            List<MOVE> moves = GenerateMoves(false);
            GenAttackMap(true);

            foreach (MOVE mv in movesDone)
                moves.Remove(mv);
            if (moveType == 2)
                moves.RemoveAll(x => x.pcDst == 0);

            for (int i = 0; i < moves.Count; i++)
            {
                MOVE mv = moves[i];
                mv.checking = IsChecking(mv);
                if (mv.checking)
                {
                    //encourage continuous check
                    if (stepList.Count >= 2 && stepList[stepList.Count - 2].move.checking)
                        mv.score += 30;
                    else
                        mv.score += 10;
                    //Avoid repeating check by the same piece 
                    if (mv.pcDst == 0 && stepList.Count >= 2 && mv.sqSrc == stepList[stepList.Count - 2].move.sqDst)
                        mv.score -= 5;
                }
            }

            //usually, moveType is either 3 or 7
            switch (moveType)
            {
                case 1:
                    moves.RemoveAll(x => x.checking == false);
                    break;
                case 2:
                    moves.RemoveAll(x => x.pcDst == 0);
                    break;
                case 3:
                    moves.RemoveAll(x => x.checking == false && x.pcDst == 0);
                    break;
                default:
                    break;
            }

            for (int i = 0; i < moves.Count; i++)
            {
                MOVE mv = moves[i];
                int vl = SEE(mv, AttackMap[sdPlayer, mv.sqDst], AttackMap[1 - sdPlayer, mv.sqDst]);
                mv.score += vl;
                mv.score += PiecePositionValue(mv.pcSrc, mv.sqDst) - PiecePositionValue(mv.pcSrc, mv.sqSrc);
                if (vl > 0)
                {
                    mv.score += GoodScore;
                    mv.killer = KILLER.GoodCapture;
                }
                else if (vl == 0)
                {
                    int j = GetHistoryIndex(mv);
                    Debug.Assert(HistHit[j] <= HistTotal[j]);
                    mv.score += (HistHit[j] + 1) * History[j] / (HistTotal[j] + 1) + HistoryScore;
                    Debug.Assert(mv.score < 0);
                    mv.killer = KILLER.Normal;
                }
                else
                {
                    mv.score += BadScore;
                    Debug.Assert(mv.score < HistoryScore);
                    mv.killer = KILLER.BadCapture;
                }
            }
            //don't extend bad capture in quiescence search
            if (!wantAll)
                moves.RemoveAll(x => x.score < BadScore);

            //assign killer bonus 
            if (mate_threat > 0)
            {
                killer = moves.Find(x => x == Killers[mate_threat + 1, 0]);
                if (!(killer is null))
                {
                    killer.score += TransScore - 1;
                    killer.killer = KILLER.Mate;
                }
            }
            if (!(Killers[height, 0] is null) && Killers[height, 0].sqSrc > 0)
            {
                killer = moves.Find(x => x == Killers[height, 0]);
                if (!(killer is null))
                {
                    killer.score += KillerScore;
                    killer.killer = KILLER.Killer1;
                    killer = moves.Find(x => x == Killers[height, 1]);
                    if (!(killer is null))
                    {
                        killer.killer = KILLER.Killer2;
                        killer.score += KillerScore - 1;
                    }
                }
            }
            moves.Sort(Large2Small);
            foreach (var mv in moves)
                yield return mv;

            int PiecePositionValue(int pc, int sq)
            {
                Debug.Assert(pc < 48 && pc > 15);
                int sd = SIDE(pc);
                int sqMirror = sd == 0 ? sq : SQUARE_FLIP(sq);
                int sqOppKing;
                int vl;
                switch (cnPieceKinds[pc])
                {
                    case ROOK:
                        sqOppKing = sqPieces[OPP_SIDE_TAG(sd) + KING_FROM];
                        vl = cRookValue[sqMirror];
                        if (SAME_FILE(sq, sqOppKing) || SAME_RANK(sq, sqOppKing))
                            vl += 5;
                        return vl;
                    case CANNON:
                        sqOppKing = sqPieces[OPP_SIDE_TAG(sd) + KING_FROM];
                        if (SAME_FILE(sq, sqOppKing))
                            return 10;
                        else if (SAME_RANK(sq, sqOppKing))
                            return 8;
                        break;
                    case KNIGHT:
                        vl = cKnightValue[sqMirror];
                        //检查绊马腿
                        for (int j = 0; j < 4; j++)
                        {
                            int sqPin = sq + ccKingDelta[j];
                            if (pcSquares[sqPin] != 0)
                                vl -= 8;
                        }
                        return vl;
                    case PAWN:
                        return cKingPawnValue[sqMirror];
                    case KING:
                        return cKingPawnValue[sqMirror];
                    case BISHOP:
                    case GUARD:
                        return cBishopGuardValue[sqMirror];
                    default:
                        Debug.Fail("Unknown piece type");
                        break;
                }
                return 0;
            }
        }

        int Large2Small(MOVE a, MOVE b)
        {
            return b.score.CompareTo(a.score);
        }

        bool IsChecking(MOVE mv)
        {
            //discovered check
            int discover = DiscoverAttack[mv.pcSrc];
            if (discover > 0)
            {
                if (discover == 3)
                    return true;
                if (discover == 1 && !SAME_FILE(mv.sqSrc, mv.sqDst))
                    return true;
                if (discover == 2 && !SAME_RANK(mv.sqSrc, mv.sqDst))
                    return true;
            }

            //direct check
            int side = SIDE(mv.pcSrc);
            int sqOppking = sqPieces[OPP_SIDE_TAG(side) + KING_FROM];
            MovePiece(mv);
            if (IsLegalMove(mv.sqDst, sqOppking))
            {
                UndoMovePiece(mv);
                mv.checking = true;
                return true;
            }
            UndoMovePiece(mv);
            return false;
        }

        public void GenMoveTest()
        {
            foreach (var mv in GetNextMove(7, 0))
                Console.WriteLine($"{mv}\t{mv.score}");
            Console.WriteLine("End of moves");
        }

        int PieceValue(int pc)
        {
            if (cnPieceKinds[pc] == PAWN)
            {
                int sq = sqPieces[pc];
                if (HOME_HALF[SIDE(pc), sq])
                    return MAT_PAWN;
                else
                    return MAT_PASSED_PAWN;
            }
            else
                return cnPieceValue[pc];
        }

        //Static Exchange Evaluation
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
            MovePiece(mv);
            Debug.Assert(CheckedBy(1 - sdPlayer) == 0);
            //find defending slider piece behind the attacking piece
            int attKind = cnPieceKinds[mv.pcSrc];
            bool potentialHideSlider = attKind == PAWN || attKind == ROOK || attKind == KING;
            if (potentialHideSlider)
            {
                foreach (int pc in AttackMap[sdPlayer, mv.sqSrc])
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
                for (int i = 0; i < defs.Count; i++)
                {
                    int pc = defs[i];
                    if (cnPieceKinds[pc] == CANNON)
                    {
                        int sq = sqPieces[pc];
                        if ((SAME_FILE(sq, mv.sqSrc) || SAME_RANK(sq, mv.sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sq))
                        {
                            defs.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            int vlDst = PieceValue(mv.pcDst);
            if (defs.Count == 0)
            {
                UndoMovePiece(mv);
                return vlDst;
            }
            if (!(cnPieceKinds[mv.pcSrc] == CANNON && mv.pcDst == 0)) //only for tier 1
                atts.Remove(mv.pcSrc);
            if (potentialHideSlider)
            {
                foreach (int pc in AttackMap[SIDE(mv.pcSrc), mv.sqSrc])
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
                for (int i = 0; i < atts.Count; i++)
                {
                    int pc = atts[i];
                    if (cnPieceKinds[pc] == CANNON)
                    {
                        int sq = sqPieces[pc];
                        if ((SAME_FILE(sq, mv.sqSrc) || SAME_RANK(sq, mv.sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sq))
                        {
                            atts.RemoveAt(i);
                            break;
                        }
                    }
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
                foreach (int pc in AttackMap[sdPlayer, mv.sqSrc])
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
                for (int i = 0; i < defs.Count; i++)
                {
                    int pc = defs[i];
                    if (cnPieceKinds[pc] == CANNON)
                    {
                        int sq = sqPieces[pc];
                        if ((SAME_FILE(sq, mv.sqSrc) || SAME_RANK(sq, mv.sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sq))
                        {
                            defs.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            int vlDst = PieceValue(mv.pcDst);
            if (defs.Count == 0)
            {
                UndoMovePiece(mv);
                return vlDst;
            }
            atts.Remove(mv.pcSrc);
            if (potentialHideSlider)
            {
                foreach (int pc in AttackMap[SIDE(mv.pcSrc), mv.sqSrc])
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
                for (int i = 0; i < atts.Count; i++)
                {
                    int pc = atts[i];
                    if (cnPieceKinds[pc] == CANNON)
                    {
                        int sq = sqPieces[pc];
                        if ((SAME_FILE(sq, mv.sqSrc) || SAME_RANK(sq, mv.sqDst)) && Math.Sign(sqDst - mv.sqSrc) == Math.Sign(mv.sqSrc - sq))
                        {
                            atts.RemoveAt(i);
                            break;
                        }
                    }
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
