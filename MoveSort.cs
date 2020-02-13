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

        void SetBestMove(MOVE mv)
        {
            if (killers[depth, 0] != mv)
            {
                killers[depth, 1] = killers[depth, 0];
                killers[depth, 0] = mv;
            }
            history[mv.sqSrc, mv.sqDst] += depth * depth;
        }

        IEnumerable<MOVE> GetNextMove()
        {
            MOVE mv;
            mv = killers[depth, 0];
            if (mv.pcSrc==pcSquares[mv.sqSrc] && mv.pcDst == pcSquares[mv.sqDst] && IsLegalMove(mv.sqSrc, mv.sqDst))
                if (!CheckCheck(mv))
                    yield return mv;
            mv = killers[depth, 1];
            if (mv.pcSrc == pcSquares[mv.sqSrc] && mv.pcDst == pcSquares[mv.sqDst] && IsLegalMove(mv.sqSrc, mv.sqDst))
                if (!CheckCheck(mv))
                    yield return mv;

            IEnumerable<MOVE> moves = EnumGenerateMoves();
            foreach (MOVE mv0 in moves)
                if (!CheckCheck(mv0))
                    yield return mv0;

            bool CheckCheck(MOVE m)
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
                        return true;
                    }
                }
                // 如果移动后被将军了，那么着法是非法的，撤消该着法
                if (CheckedBy(mySide) > 0)
                {
                    UndoMovePiece(m);
                    return true;
                }
                UndoMovePiece(m);
                return false;
            }
        }


        public void GenMoveTest()
        {
            foreach (MOVE mv in GenerateMoves())
                Console.WriteLine(mv);
            Console.WriteLine("End of moves");
        }

    }
}
