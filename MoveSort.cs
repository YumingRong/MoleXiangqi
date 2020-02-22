using System;
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

        void SetBestMove(MOVE mv, int score,int depthleft)
        {
            Debug.Assert(mv.sqSrc != 0);
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
            int[] scores = new int[moves.Count];
            int[] kinds = new int[moves.Count];//check, capture or normal move
            
            //assign killer bonus
            if (Killers[sdPlayer, 0].sqSrc > 0)
            {
                int j = moves.FindIndex(x => x == Killers[sdPlayer, 0]);
                if (j > -1)
                    scores[j] = 15;
                if (Killers[sdPlayer, 0].sqSrc>0)
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
                    //if capture last moving piece, it's probably a good capture
                    if (mv.sqDst == stepList[stepList.Count - 1].move.sqDst)
                        scores[i] += cnPieceValue[mv.pcDst];
                    else
                        scores[i] += cnPieceValue[mv.pcDst] / 2;
                }
                else if (wantAll)
                {
                    scores[i] += HistHit[cnPieceHistIndex[mv.pcSrc], mv.sqDst] * HistoryMax / (HistTotal[cnPieceHistIndex[mv.pcSrc], mv.sqDst] + 1) + HistoryScore;
                    kinds[i] |= 4;
                }
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
                else if (mv.pcDst > 0)
                {
                    //if capture last moving piece, it's probably a good capture
                    if (mv.sqDst == stepList[stepList.Count - 1].move.sqDst)
                        scores[i] += cnPieceValue[mv.pcDst];
                    else
                        scores[i] += cnPieceValue[mv.pcDst] / 2;
                }
                scores[i] += History[cnPieceKinds[mv.pcSrc], mv.sqDst] - 5000;
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

    }
}
