using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            history[mv.sqSrc, mv.sqDst] += depth * depth;
            if (score > G.WIN)
                MateKiller = mv;
        }

        /*该函数首先返回matekiller，然后返回killer move
         * 对于其它着法，按照将、吃子、移动的优先级返回
         * 参数onlyCheckCapture: 
         * 0 - 生成所有合法着法
         * 1 - 生成所有照将
         * 2 - 生成所有吃子
         * 3 - 生成所有照将和吃子
         */
        IEnumerable<MOVE> GetNextMove(int onlyCheckCapture)
        {
            bool wantCheck = (onlyCheckCapture & 0x01) > 0;
            bool wantCapture = (onlyCheckCapture & 0x02) > 0;
            bool wantAll = onlyCheckCapture == 0;

            List<MOVE> allkillers = new List<MOVE>();
            if (MateKiller.sqSrc > 0)
                allkillers.Add(MateKiller);
            if (killers[depth, 0].sqSrc > 0)
            {
                allkillers.Add(killers[depth, 0]);
                if (killers[depth, 1].sqSrc > 0)
                    allkillers.Add(killers[depth, 1]);
            }

            foreach (MOVE mv in allkillers)
            {
                if (mv.pcSrc == pcSquares[mv.sqSrc] && mv.pcDst == pcSquares[mv.sqDst] && IsLegalMove(mv.sqSrc, mv.sqDst))
                {
                    Tuple<bool, int> cc = CheckedChecking(mv);
                    if (!cc.Item1)
                    {
                        if (wantAll || wantCheck && cc.Item2 > 0 || wantCapture && mv.pcDst > 0)
                            yield return mv;
                    }
                }
            }

            List<MOVE> moves = GenerateMoves();
            List<MOVE> captureMoves = new List<MOVE>();
            List<MOVE> normalMoves = new List<MOVE>();
            foreach (MOVE mv in moves)
            {
                Tuple<bool, int> cc = CheckedChecking(mv);
                if (!cc.Item1)
                {
                    if (wantCheck && cc.Item2 > 0)
                        yield return mv;
                    if (wantCapture && mv.pcDst > 0)
                        captureMoves.Add(mv);
                    else if (wantAll)
                        normalMoves.Add(mv);
                }
            }
            if (wantCapture)
                foreach (MOVE m in captureMoves)
                    yield return m;
            if (wantAll)
                foreach (MOVE m in normalMoves)
                    yield return m;

            Tuple<bool, int> CheckedChecking(MOVE m)
            {
                int sqCheck = stepList[stepList.Count - 1].checking;
                int mySide = sdPlayer;
                MovePiece(m);
                if (sqCheck > 0)
                {
                    int sqKing = sqPieces[SIDE_TAG(mySide) + KING_FROM];
                    //如果被照将，先试试走棋后，照将着法是否仍然成立
                    if (IsLegalMove(sqCheck, sqKing))
                    {
                        UndoMovePiece(m);
                        return new Tuple<bool, int>(true, 0);
                    }
                }
                // 如果移动后被将军了，那么着法是非法的
                if (CheckedBy(mySide) > 0)
                {
                    UndoMovePiece(m);
                    return new Tuple<bool, int>(true, 0);
                }

                int sqchecking = CheckedBy(1 - mySide);
                UndoMovePiece(m);
                return new Tuple<bool, int>(false, sqchecking);
            }
        }

        public List<KeyValuePair<MOVE, int>> InitRootMoves()
        {
            List<MOVE> moves = GenerateMoves();
            List<KeyValuePair<MOVE, int>> rmoves = new List<KeyValuePair<MOVE, int>>();
            foreach (MOVE mv in moves)
            {
                MovePiece(mv);
                int score = 0;
                if (CheckedBy(1 - sdPlayer) == 0)
                {
                    if (CheckedBy(sdPlayer) > 0)
                        score = 100;
                    rmoves.Add(new KeyValuePair<MOVE, int>(mv, score));
                }
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
