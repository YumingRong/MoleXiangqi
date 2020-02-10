using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoleXiangqi
{
    partial class SEARCH
    {
        MOVE[,] killers;
        int[,] history;

        void SetBestMove(MOVE mv, int depth)
        {
            history[mv.sqSrc, mv.sqDst] += depth * depth;
            killers[depth, 1] = killers[depth, 0];
            killers[depth, 0] = mv;
        }
    }
}
