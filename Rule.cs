using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//凡是以i开头的变量都是为了与图形界面沟通使用的

namespace MoleXiangqi
{
    partial class POSITION
    {
        public int halfMoveClock;  //120步不吃子作和的自然限招

        enum RepititionResult {WIN, DRAW, LOSE, NONE };

        // 重复局面检测。完美支持亚洲规则
        public RepititionResult Repitition()
        {
            int repStart = -1;
            int nstep = stepList.Count - 1;
            // 1. 首先检测历史局面中是否有当前局面，如果没有，就用不着判断了
            for (int i = nstep - 1; i >= 0 && !stepList[i].capture; i--)
            {
                if (stepList[i].zobrist == stepList[nstep].zobrist)
                {
                    repStart = i;
                    break;
                }
            }
            if (repStart < 0)
                return RepititionResult.NONE;
            // 2. 判断长照
            bool bPerpCheck = true;
            for (int i = stepList.Count - 2; i >= repStart; i -= 2)
            {
                if (stepList[i].checking == 0)
                {
                    bPerpCheck = false;
                    break;
                }
            }
            if (bPerpCheck)
            {
                bool bOppPerpCheck = true;
                for (int i = nstep - 2; i >= repStart; i -= 2)
                {
                    if (stepList[i].checking>0)
                    {
                        bOppPerpCheck = false;
                        break;
                    }
                }
                //双方循环长将（解将反将）作和
                if (bOppPerpCheck)
                    return RepititionResult.DRAW;
                //无论任何情况，单方一子长将或多子交替长将者均需变着，不变作负
                //二将一还将，长将者作负
                else
                    return RepititionResult.WIN;
            }
            //3. 倒退棋局到上一次重复局面
            Stack<MOVE> rollback = new Stack<MOVE>();
            for (int i = nstep; i >= repStart; i--)
            {
                MOVE mv = moveStack.Peek();
                rollback.Push(mv);
                UndoMovePiece(mv);
            }
            //先假设所有的棋子都被长捉，然后从集合里逐个排除
            int sd, sq;
            SortedSet<int>[] PerpChase = new SortedSet<int>[2];
            for (sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                for (int pc = bas + ROOK_FROM; pc <= bas + KNIGHT_TO; pc++)
                    if (sqPieces[pc] > 0)
                        PerpChase[sd].Add(pc);

                for (int pc = bas + PAWN_FROM; pc <= bas + PAWN_TO; pc++)
                {
                    sq = sqPieces[pc];
                    //攻击未过河的兵不算捉
                    if (sq > 0 && HOME_HALF[sd, sq])
                        PerpChase[sd].Add(pc);
                }
                for (int pc = bas + BISHOP_FROM; pc <= bas + GUARD_TO; pc++)
                    if (sqPieces[pc] > 0)
                        PerpChase[sd].Add(pc);
            }

            for (int i = repStart;i<nstep;i++)
            {
                sd = sdPlayer;
                foreach (int c in PerpChase[sd])
                {
                    if (!)
                }
            }
        }

    }
}
