using System;
using System.Collections.Generic;

namespace MoleXiangqi
{
    partial class POSITION
    {
        internal MOVE[,] killers;
        internal int[,] history;
        internal MOVE MateKiller;

        void SetBestMove(MOVE mv, int score)
        {
            if (killers[depth, 0] != mv)
            {
                killers[depth, 1] = killers[depth, 0];
                killers[depth, 0] = mv;
            }
            history[cnPieceTypes[mv.pcSrc], mv.sqDst] += depth * depth;
            if (score > G.WIN)
                MateKiller = mv;
        }

        /*该函数首先返回matekiller，然后返回killer move
         * 对于其它着法，按吃子、照将、移动的优先级打分
         * 参数onlyCheckCapture: 
         * 1 - 生成所有照将
         * 2 - 生成所有吃子
         * 3 - 生成所有照将和吃子
         * 7 - 生成所有合法着法
        */
        public IEnumerable<MOVE> GetNextMove(int moveType)
        {
            bool wantCheck = (moveType & 0x01) > 0;
            bool wantCapture = (moveType & 0x02) > 0;
            bool wantAll = moveType == 7;

            List<MOVE> killerList = new List<MOVE>();
            List<MOVE> killerDone = new List<MOVE>();
            if (MateKiller.sqSrc > 0)
                killerList.Add(MateKiller);
            if (killers[depth, 0].sqSrc > 0)
            {
                killerList.Add(killers[depth, 0]);
                if (killers[depth, 1].sqSrc > 0)
                    killerList.Add(killers[depth, 1]);
            }

            foreach (MOVE mv in killerList)
            {
                //Warning! check is not tested here. To be done later. 
                if (mv.pcSrc == pcSquares[mv.sqSrc] && mv.pcDst == pcSquares[mv.sqDst] && IsLegalMove(mv.sqSrc, mv.sqDst))
                {
                    MovePiece(mv);
                    if (CheckedBy(1 - sdPlayer) > 0)
                    {
                        UndoMovePiece(mv);
                        continue;
                    }
                    UndoMovePiece(mv);
                    if (wantAll || wantCheck && IsChecking(mv) || wantCapture && mv.pcDst > 0)
                    {
                        killerDone.Add(mv);
                        yield return mv;
                    }
                }
            }

            List<MOVE> moves = GenerateMoves();
            //remove killers already done
            foreach (MOVE mv in killerDone)
                moves.Remove(mv);

            int[] scores = new int[moves.Count];
            int[] kinds = new int[moves.Count];//check, capture or normal move
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
                else if (mv.pcDst > 0)
                {
                    kinds[i] |= 2;
                    //if capture last moving piece, it's probably a good capture
                    if (mv.sqDst == stepList[stepList.Count - 1].move.sqDst)
                        scores[i] += cnPieceValue[mv.pcDst];
                    else
                        scores[i] += cnPieceValue[mv.pcDst] / 2;
                }
                else if (wantAll)
                {
                    scores[i] += history[cnPieceKinds[mv.pcSrc], mv.sqDst] - 500;
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
                scores[i] += history[cnPieceKinds[mv.pcSrc], mv.sqDst] - 5000;
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
