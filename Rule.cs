using System;
using System.Collections.Generic;
//凡是以i开头的变量都是为了与图形界面沟通使用的

namespace MoleXiangqi
{
    public enum RepititionResult { WIN = 4900, DRAW = 0, LOSE = -4900, NONE };

    partial class POSITION
    {
        // 重复局面检测。支持亚洲规则
        public RepititionResult Repitition()
        {
            int repStart = -1;
            int nstep = stepList.Count - 1;
            // 1. 首先检测历史局面中是否有当前局面，如果没有，就用不着判断了
            for (int i = nstep - 4; i >= nstep - stepList[nstep].halfMoveClock; i -= 2)
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
            for (int i = nstep; i >= repStart; i -= 2)
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
                for (int i = nstep - 1; i >= repStart; i -= 2)
                {
                    if (stepList[i].checking == 0)
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
            for (int i = nstep; i >= repStart; i--)
                UndoMovePiece(stepList[i].move);
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
                    if (sq > 0 && HOME_HALF[1 - sd, sq])
                        PerpChase[sd].Add(pc);
                }
                for (int pc = bas + BISHOP_FROM; pc <= bas + GUARD_TO; pc++)
                    if (sqPieces[pc] > 0)
                        PerpChase[sd].Add(pc);
            }

            for (int i = repStart; i <= nstep; i++)
            {
                GenAttackMap();

                //一子轮捉两子或多子作和。两子分别轮捉两子或多子亦作和局
                //本方被捉只在偶数层有，奇数层没有；对方被捉只在奇数层有，偶数层没有
                foreach (int c in PerpChase[sdPlayer])
                {
                    if (!Chased(c))
                        PerpChase[sd].Remove(c);
                }

                //对方的棋子在此层应该不被捉
                foreach (int c in PerpChase[1 - sdPlayer])
                    if (Chased(c))
                        PerpChase[1 - sdPlayer].Remove(c);
                MovePiece(stepList[i].move);
            }

            //如果双方均长捉违例，则判和
            if (PerpChase[sdPlayer].Count > 0 && PerpChase[1 ^ sdPlayer].Count > 0)
                return RepititionResult.DRAW;
            //单方面长捉判负
            if (PerpChase[sdPlayer].Count > 0)
                return RepititionResult.WIN;

            //默认不变作和
            return RepititionResult.DRAW;
        }

        //这里的捉指亚洲规则的捉，调用此函数前必须先执行GenAttMap()
        bool Chased(int pc)
        {
            int sq = sqPieces[pc];
            int sd = SIDE(pc);
            int sdOpp = 1 - sd;
            int pcOpp = attackMap[sdOpp, sq];
            if (pcOpp == 0)
                return false;  //没有捉子，排除
            else
            {
                int sqOpp = sqPieces[pcOpp];
                if (cnPieceKinds[pcOpp] == PIECE_PAWN || cnPieceKinds[pcOpp] == PIECE_KING)
                    return false;  //兵和将允许长捉其它子
                if (attackMap[sdOpp, sqOpp] == pc)
                    return false;  	//两子互捉，算成兑子，作和
                if (cnPieceValue[pc] <= cnPieceValue[pcOpp] && attackMap[sd, sq] > 0)
                    return false;   //攻击有根子不算捉，除非被攻击子价值更大
            }
            return true;
        }

        //该函数等同于Evaluate的内部函数，只是去掉检查absolute pin和位置数组赋值，以提高速度，并减少耦合
        //如发现任何bug，须一同修改
        void GenAttackMap()
        {
            int sqSrc, sqDst, delta, pcDst;
            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                int sqOppKing = sqPieces[OPP_SIDE_TAG(sd) + KING_FROM];
                for (int pc = bas; pc < bas + 16; pc++)
                {
                    sqSrc = sqPieces[pc];
                    if (sqSrc == 0)
                        continue;
                    int pcKind = cnPieceKinds[pc];
                    int sqSrcMirror = sd == 0 ? sqSrc : SQUARE_FLIP(sqSrc);

                    switch (pcKind)
                    {
                        case PIECE_KING:
                            for (int i = 0; i < 4; i++)
                            {
                                sqDst = sqSrc + ccKingDelta[i];
                                if (IN_FORT[sqDst])
                                    attackMap[sd, sqDst] = pc;
                            }
                            break;
                        case PIECE_ROOK:
                            for (int j = 0; j < 4; j++)
                            {
                                delta = ccKingDelta[j];
                                for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                                {
                                    pcDst = pcSquares[sqDst];
                                    attackMap[sd, sqDst] = pc;
                                    if (pcDst != 0)
                                        break;
                                }
                            }
                            break;
                        case PIECE_CANNON:
                            for (int j = 0; j < 4; j++)
                            {
                                int nDelta = ccKingDelta[j];
                                for (sqDst = sqSrc + nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                                {
                                    if (pcSquares[sqDst] != 0) //炮架
                                    {
                                        for (sqDst += nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                                        {
                                            attackMap[sd, sqDst] = pc;
                                            if (pcSquares[sqDst] != 0) //直瞄点
                                                goto NextFor;
                                        }
                                    }
                                }
                            NextFor:;
                            }
                            break;
                        case PIECE_KNIGHT:
                            for (int j = 0; j < 4; j++)
                            {
                                if (pcSquares[sqSrc + ccKingDelta[j]] == 0)
                                {
                                    attackMap[sd, sqSrc + ccKnightDelta[j, 0]] = pc;
                                    attackMap[sd, sqSrc + ccKnightDelta[j, 1]] = pc;
                                }
                            }
                            break;
                        case PIECE_PAWN:
                            attackMap[sd, SQUARE_FORWARD(sqSrc, sd)] = pc;
                            if (HOME_HALF[1 - sd, sqSrc])
                            {
                                attackMap[sd, sqSrc + 1] = pc;
                                attackMap[sd, sqSrc - 1] = pc;
                            }
                            break;
                        case PIECE_BISHOP:
                            for (int j = 0; j < 4; j++)
                            {
                                sqDst = sqSrc + ccGuardDelta[j];
                                if (HOME_HALF[sd, sqDst] && pcSquares[sqDst] == 0)
                                    attackMap[sd, sqDst + ccGuardDelta[j]] = pc;
                            }
                            break;
                        case PIECE_GUARD:
                            for (int j = 0; j < 4; j++)
                            {
                                sqDst = sqSrc + ccGuardDelta[j];
                                if (IN_FORT[sqDst])
                                    attackMap[sd, sqDst] = pc;
                            }
                            break;
                        default:
                            break;
                    }
                }
                //帅所在的格没有保护
                attackMap[sd, sqPieces[bas + KING_FROM]] = 0;
            }

        }

    }
}
