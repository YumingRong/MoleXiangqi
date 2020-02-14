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
         */
        IEnumerable<MOVE> GetNextMove()
        {
            MOVE mv;
            mv = MateKiller;
            if (mv.pcSrc == pcSquares[mv.sqSrc] && mv.pcDst == pcSquares[mv.sqDst] && IsLegalMove(mv.sqSrc, mv.sqDst))
                if (!CheckedChecking(mv).Item1)
                    yield return mv;

            mv = killers[depth, 0];
            if (mv.pcSrc == pcSquares[mv.sqSrc] && mv.pcDst == pcSquares[mv.sqDst] && IsLegalMove(mv.sqSrc, mv.sqDst))
                if (!CheckedChecking(mv).Item1)
                    yield return mv;
            mv = killers[depth, 1];
            if (mv.pcSrc == pcSquares[mv.sqSrc] && mv.pcDst == pcSquares[mv.sqDst] && IsLegalMove(mv.sqSrc, mv.sqDst))
                if (!CheckedChecking(mv).Item1)
                    yield return mv;

            List<MOVE> moves = GenerateMoves();
            List<MOVE> captureMoves = new List<MOVE>();
            List<MOVE> normalMoves = new List<MOVE>();
            foreach (MOVE m in moves)
            {
                Tuple<bool, int> cc = CheckedChecking(m);
                if (!cc.Item1)
                {
                    if (cc.Item2 > 0)
                        yield return m;
                    else if (m.pcDst > 0)
                        captureMoves.Add(m);
                    else
                        normalMoves.Add(m);
                }
            }
            foreach (MOVE m in captureMoves)
                yield return m;
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


                UndoMovePiece(m);
                return new Tuple<bool, int>(false, CheckedBy(1 - mySide));
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
