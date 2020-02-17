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
         * 对于其它着法，按照将、吃子、移动的优先级返回
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

            List<MOVE> moves = new List<MOVE>();
            //if (MateKiller.sqSrc > 0)
            //    moves.Add(MateKiller);
            //if (killers[depth, 0].sqSrc > 0)
            //{
            //    moves.Add(killers[depth, 0]);
            //    if (killers[depth, 1].sqSrc > 0)
            //        moves.Add(killers[depth, 1]);
            //}

            //foreach (MOVE mv in moves)
            //{
            //    //Warning! check is not tested here. To be done later. 
            //    if (mv.pcSrc == pcSquares[mv.sqSrc] && mv.pcDst == pcSquares[mv.sqDst] && IsLegalMove(mv.sqSrc, mv.sqDst))
            //    {
            //        if (wantAll || wantCheck && IsChecking(mv) || wantCapture && mv.pcDst > 0)
            //            yield return mv;
            //    }
            //}

            moves = GenerateMoves();
            List<MOVE> captureMoves = new List<MOVE>();
            List<MOVE> normalMoves = new List<MOVE>();
            //为避免一子淡水长将，能多子轮流照将就轮流将
            MOVE lateCheck = new MOVE();
            foreach (MOVE mv in moves)
            {
                if (wantCheck && IsChecking(mv))
                {
                    if (mv.pcDst == 0 && mv.sqSrc == stepList[stepList.Count - 2].checking)
                        lateCheck = mv;
                    else
                        yield return mv;
                }
                else if (mv.pcDst > 0)
                    captureMoves.Add(mv);
                else if (wantAll)
                    normalMoves.Add(mv);
            }
            if (wantCheck && lateCheck.sqSrc > 0)
                yield return lateCheck;
            if (wantCapture)
                foreach (MOVE m in captureMoves)
                    yield return m;
            if (wantAll)
                foreach (MOVE m in normalMoves)
                    yield return m;

            bool IsChecking(MOVE m)
            {
                int mySide = sdPlayer;
                MovePiece(m);
                int sqchecking = CheckedBy(1 - mySide);
                UndoMovePiece(m);
                return sqchecking > 0;
            }
        }

        public List<KeyValuePair<MOVE, int>> InitRootMoves()
        {
            int[] oppAttackMap = GenAttackMap(1 - sdPlayer, FindAbsolutePin(1 - sdPlayer));
            List<MOVE> moves = GenerateMoves();
            List<KeyValuePair<MOVE, int>> rmoves = new List<KeyValuePair<MOVE, int>>();
            foreach (MOVE mv in moves)
            {
                MovePiece(mv);
                int score = 0;
                //check bonus
                if (CheckedBy(sdPlayer) > 0)
                    score += 500;

                //capture bonus
                if (mv.pcDst > 0)
                    score += cnPieceValue[mv.pcDst];
                if (oppAttackMap[mv.sqDst] > 0)
                    score -= cnPieceValue[mv.pcSrc];
                rmoves.Add(new KeyValuePair<MOVE, int>(mv, score));
                UndoMovePiece(mv);
            }
            rmoves.Sort(SortLarge2Small);
            return rmoves;
        }

        public void GenMoveTest()
        {
            foreach (MOVE mv in GenerateMoves())
                Console.WriteLine(mv);
            Console.WriteLine("End of moves");
        }

    }
}
