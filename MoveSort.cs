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
            if (IsLegalMove(mv.sqSrc, mv.sqDst))
                yield return mv;
            mv = killers[depth, 1];
            if (IsLegalMove(mv.sqSrc, mv.sqDst))
                yield return mv;
        }

        public void GenMoveTest(string fen)
        {
            FromFEN(fen);
            foreach (MOVE mv in Complex_GenMoves())
                Console.WriteLine(mv);
            Console.WriteLine("End of moves");
        }
    }
}
