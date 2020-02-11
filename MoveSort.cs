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
            Complex_Evaluate();
            captureMoves.Sort(SortLarge2Small);
            foreach (KeyValuePair<MOVE, int> mv_vl in captureMoves)
                yield return mv_vl.Key;

        }


    }
}
