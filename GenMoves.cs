using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    public partial class POSITION
    {
        // 帅(将)的步长
        static readonly int[] ccKingDelta = { -0x10, +0x10, -0x01, +0x01 };
        // 仕(士)的步长
        static readonly int[] ccGuardDelta = { -0x11, +0x11, -0x0f, +0x0f };
        // 马的步长，以帅(将)的步长作为马腿
        static readonly int[,] ccKnightDelta = { { -33, -31 }, { 31, 33 }, { -18, 14 }, { -14, 18 } };
        // 马被将军的步长，以仕(士)的步长作为马腿
        static readonly int[,] ccKnightCheckDelta = { { -33, -18 }, { 18, 33 }, { -31, -14 }, { 14, 31 } };
        //[pin, direction]，用来判断棋子运动方向受不受牵制
        static readonly bool[,] ccPinDelta = { { false, false, false, false }, { false, false, true, true, }, { true, true, false, false }, { true, true, true, true } };

        public bool IsLegalMove(int sqSrc, int sqDst)
        {
            int sqPin, pcMoved, pcCaptured, selfSide;
            int delta;
            // 着法合理性检测包括以下几个步骤：

            // 1. 检查要走的子是否存在
            if (!IN_BOARD[sqSrc] || !IN_BOARD[sqDst])
                return false;
            pcMoved = pcSquares[sqSrc];
            if (pcMoved == 0)
                return false;
            selfSide = SIDE(pcMoved);

            // 2. 检查吃到的子是否为对方棋子(如果有吃子的话)
            pcCaptured = pcSquares[sqDst];
            if (SIDE(pcCaptured) == selfSide)
                return false;

            switch (cnPieceKinds[pcMoved])
            {
                // 3. 如果是帅(将)或仕(士)，则先看是否在九宫内，再看是否是合理位移
                case KING:
                    return IN_FORT[sqDst] && KING_SPAN(sqSrc, sqDst);
                case GUARD:
                    return IN_FORT[sqDst] && ADVISOR_SPAN(sqSrc, sqDst);

                // 4. 如果是相(象)，则先看是否过河，再看是否是合理位移，最后看有没有被塞象眼
                case BISHOP:
                    return SAME_HALF(sqSrc, sqDst) && BISHOP_SPAN(sqSrc, sqDst) && pcSquares[BISHOP_PIN(sqSrc, sqDst)] == 0;

                // 5. 如果是马，则先看看是否是合理位移，再看有没有被蹩马腿
                case KNIGHT:
                    sqPin = KNIGHT_PIN(sqSrc, sqDst);
                    return sqPin != sqSrc && pcSquares[sqPin] == 0;

                // 6. 如果是车，则先看是横向移动还是纵向移动
                case ROOK:
                    if (SAME_RANK(sqSrc, sqDst))
                    {
                        delta = (sqDst < sqSrc ? -1 : 1);
                    }
                    else if (SAME_FILE(sqSrc, sqDst))
                    {
                        delta = (sqDst < sqSrc ? -16 : 16);
                    }
                    else
                    {
                        return false;
                    }
                    for (sqPin = sqSrc + delta; sqPin != sqDst; sqPin += delta)
                    {
                        if (pcSquares[sqPin] > 0)
                            return false;
                    }
                    return true;

                // 7. 如果是炮，判断起来和车一样
                case CANNON:
                    if (SAME_RANK(sqSrc, sqDst))
                        delta = (sqDst < sqSrc ? -1 : 1);
                    else if (SAME_FILE(sqSrc, sqDst))
                        delta = (sqDst < sqSrc ? -16 : 16);
                    else
                        return false;
                    int nPin = 0;
                    for (sqPin = sqSrc + delta; sqPin != sqDst; sqPin += delta)
                    {
                        if (pcSquares[sqPin] > 0)
                            nPin++;
                    }
                    return pcCaptured > 0 && nPin == 1 || pcCaptured == 0 && nPin == 0;

                // 8. 如果是兵(卒)，则按红方和黑方分情况讨论
                default:
                    if (sqDst == SQUARE_FORWARD(sqSrc, selfSide))
                        return true;
                    if (HOME_HALF[1 - selfSide, sqSrc] && (sqDst == sqSrc - 1 || sqDst == sqSrc + 1))
                        return true;
                    else
                        return false;
            }
        }

        // 判断是否被将军
        public int CheckedBy(int side)
        {
            int i, j, sqSrc, sqDst;
            int pcSelfSide, pcOppSide, pcDst, delta;
            pcSelfSide = SIDE_TAG(side);
            pcOppSide = OPP_SIDE_TAG(side);
            // 找到棋盘上的帅(将)，再做以下判断：
            sqSrc = sqPieces[pcSelfSide + KING_FROM];
            Debug.Assert(IN_BOARD[sqSrc]);

            //4. 判断是否将帅对脸
            if (KingsFace2Face())
                return sqPieces[pcOppSide + KING_FROM];

            // 1. 判断是否被对方的兵(卒)将军
            if (cnPieceTypes[pcSquares[SQUARE_FORWARD(sqSrc, side)]] == pcOppSide + PAWN)
                return SQUARE_FORWARD(sqSrc, side);
            if (cnPieceTypes[pcSquares[sqSrc - 1]] == pcOppSide + PAWN)
                return sqSrc - 1;
            if (cnPieceTypes[pcSquares[sqSrc + 1]] == pcOppSide + PAWN)
                return sqSrc + 1;

            // 2. 判断是否被对方的马将军(以仕(士)的步长当作马腿)
            for (i = 0; i < 4; i++)
            {
                int sqPin = sqSrc + ccGuardDelta[i];
                if (pcSquares[sqPin] == 0)
                    for (j = 0; j < 2; j++)
                    {
                        sqDst = sqSrc + ccKnightCheckDelta[i, j];
                        pcDst = pcSquares[sqDst];
                        if (cnPieceTypes[pcDst] == pcOppSide + KNIGHT)
                            return sqDst;
                    }
            }

            // 3. 判断是否被对方的车或炮将军
            for (i = 0; i < 4; i++)
            {
                delta = ccKingDelta[i];
                for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                {
                    pcDst = pcSquares[sqDst];
                    if (pcDst != 0)
                    {
                        pcDst = cnPieceTypes[pcDst];
                        if (pcDst == pcOppSide + ROOK)
                            return sqDst;
                        else
                            for (sqDst += delta; IN_BOARD[sqDst]; sqDst += delta)
                            {
                                pcDst = pcSquares[sqDst];
                                if (pcDst != 0)
                                {
                                    if (cnPieceTypes[pcDst] == pcOppSide + CANNON)
                                        return sqDst;
                                    goto NextFor3;
                                }
                            }
                    }
                }
            NextFor3:;
            }
            return 0;
        }

        //判断是否将帅对脸
        bool KingsFace2Face()
        {
            int sqSrc = sqPieces[32 + KING_FROM];
            int sqDst = sqPieces[16 + KING_FROM];
            if (SAME_FILE(sqSrc, sqDst))
            {
                for (int i = sqSrc + 16; i < sqDst; i += 16)
                    if (pcSquares[i] > 0)
                        return false;
                return true;
            }
            return false;
        }

        // 判断是否被杀
        public bool IsMate()
        {
            //List<MOVE> mvs;

            //mvs = GenerateMoves();
            foreach (MOVE mv in GenerateMoves())
            {
                //Debug.WriteLine(MOVE2ICCS(mv) + "," + SRC(mv) + "-" + DST(mv));
                MovePiece(mv);
                if (CheckedBy(1 ^ sdPlayer) == 0)
                {
                    UndoMovePiece(mv);
                    return false;
                }
                else
                {
                    UndoMovePiece(mv);
                }
            }
            return true;
        }

        //着法生成器
        List<MOVE> GenerateMoves()
        {
            int sqSrc, sqDst, pcDst;
            int pcSelfSide, pcOppSide;
            int delta;
            List<MOVE> mvs = new List<MOVE>();

            pcSelfSide = SIDE_TAG(sdPlayer);
            pcOppSide = OPP_SIDE_TAG(sdPlayer);

            for (int i = ROOK_FROM; i <= ROOK_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    delta = ccKingDelta[j];
                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                    {
                        pcDst = pcSquares[sqDst];
                        if (pcDst == 0)
                            mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, 0));
                        else
                        {
                            if ((pcDst & pcOppSide) != 0)
                                mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                            break;
                        }
                    }
                }
            }
            for (int i = CANNON_FROM; i <= CANNON_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    delta = ccKingDelta[j];
                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                    {
                        pcDst = pcSquares[sqDst];
                        if (pcDst == 0)
                            mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, 0));
                        else
                        {
                            for (sqDst += delta; IN_BOARD[sqDst]; sqDst += delta)
                            {
                                pcDst = pcSquares[sqDst];
                                if (pcDst != 0)
                                {
                                    if ((pcDst & pcOppSide) != 0)
                                        mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                                    goto NextFor1;
                                }
                            }

                        }
                    }
                NextFor1:;
                }
            }

            for (int i = KNIGHT_FROM; i <= KNIGHT_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    int sqPin = sqSrc + ccKingDelta[j];
                    if (pcSquares[sqPin] == 0)
                    {
                        sqDst = sqSrc + ccKnightDelta[j, 0];
                        pcDst = pcSquares[sqDst];
                        if (IN_BOARD[sqDst] && (pcDst & pcSelfSide) == 0)
                            mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                        sqDst = sqSrc + ccKnightDelta[j, 1];
                        pcDst = pcSquares[sqDst];
                        if (IN_BOARD[sqDst] && (pcDst & pcSelfSide) == 0)
                            mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                    }
                }
            }

            for (int i = PAWN_FROM; i <= PAWN_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                sqDst = SQUARE_FORWARD(sqSrc, sdPlayer);
                if (IN_BOARD[sqDst])
                {
                    pcDst = pcSquares[sqDst];
                    if ((pcDst & pcSelfSide) == 0)
                        mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                }
                if (HOME_HALF[1 - sdPlayer, sqSrc])
                {
                    for (delta = -1; delta <= 1; delta += 2)
                    {
                        sqDst = sqSrc + delta;
                        if (IN_BOARD[sqDst])
                        {
                            pcDst = pcSquares[sqDst];
                            if ((pcDst & pcSelfSide) == 0)
                                mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                        }
                    }
                }
            }

            for (int i = BISHOP_FROM; i <= BISHOP_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    sqDst = sqSrc + ccGuardDelta[j];
                    if (!(HOME_HALF[sdPlayer, sqDst] && pcSquares[sqDst] == 0))
                        continue;
                    sqDst += ccGuardDelta[j];
                    pcDst = pcSquares[sqDst];
                    if ((pcDst & pcSelfSide) == 0)
                        mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                }
            }

            for (int i = GUARD_FROM; i <= GUARD_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    sqDst = sqSrc + ccGuardDelta[j];
                    if (!IN_FORT[sqDst])
                    {
                        continue;
                    }
                    pcDst = pcSquares[sqDst];
                    if ((pcDst & pcSelfSide) == 0)
                        mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                }
            }

            sqSrc = sqPieces[pcSelfSide + KING_FROM];
            for (int i = 0; i < 4; i++)
            {
                sqDst = sqSrc + ccKingDelta[i];
                if (!IN_FORT[sqDst])
                    continue;
                pcDst = pcSquares[sqDst];
                if ((pcDst & pcSelfSide) == 0)
                    mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + KING_FROM, pcDst));
            }
            return mvs;
        }

        //着法生成器
        IEnumerable<MOVE> EnumGenerateMoves()
        {
            int sqSrc, sqDst, pcDst, delta;
            int pcSelfSide, pcOppSide;

            pcSelfSide = SIDE_TAG(sdPlayer);
            pcOppSide = OPP_SIDE_TAG(sdPlayer);

            for (int i = ROOK_FROM; i <= ROOK_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    delta = ccKingDelta[j];
                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                    {
                        pcDst = pcSquares[sqDst];
                        if (pcDst == 0)
                            yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, 0);
                        else
                        {
                            if ((pcDst & pcOppSide) != 0)
                                yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst);
                            break;
                        }
                    }
                }
            }
            for (int i = CANNON_FROM; i <= CANNON_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    delta = ccKingDelta[j];
                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                    {
                        pcDst = pcSquares[sqDst];
                        if (pcDst == 0)
                            yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, 0);
                        else
                        {
                            for (sqDst += delta; IN_BOARD[sqDst]; sqDst += delta)
                            {
                                pcDst = pcSquares[sqDst];
                                if (pcDst != 0)
                                {
                                    if ((pcDst & pcOppSide) != 0)
                                        yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst);
                                    goto NextFor1;
                                }
                            }

                        }
                    }
                NextFor1:;
                }
            }

            for (int i = KNIGHT_FROM; i <= KNIGHT_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    int sqPin = sqSrc + ccKingDelta[j];
                    if (pcSquares[sqPin] == 0)
                    {
                        sqDst = sqSrc + ccKnightDelta[j, 0];
                        pcDst = pcSquares[sqDst];
                        if (IN_BOARD[sqDst] && (pcDst & pcSelfSide) == 0)
                            yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst);
                        sqDst = sqSrc + ccKnightDelta[j, 1];
                        pcDst = pcSquares[sqDst];
                        if (IN_BOARD[sqDst] && (pcDst & pcSelfSide) == 0)
                            yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst);
                    }
                }
            }

            for (int i = PAWN_FROM; i <= PAWN_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                sqDst = SQUARE_FORWARD(sqSrc, sdPlayer);
                if (IN_BOARD[sqDst])
                {
                    pcDst = pcSquares[sqDst];
                    if ((pcDst & pcSelfSide) == 0)
                        yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst);
                }
                if (HOME_HALF[1 - sdPlayer, sqSrc])
                {
                    for (delta = -1; delta <= 1; delta += 2)
                    {
                        sqDst = sqSrc + delta;
                        if (IN_BOARD[sqDst])
                        {
                            pcDst = pcSquares[sqDst];
                            if ((pcDst & pcSelfSide) == 0)
                                yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst);
                        }
                    }
                }
            }

            for (int i = BISHOP_FROM; i <= BISHOP_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    sqDst = sqSrc + ccGuardDelta[j];
                    if (!(HOME_HALF[sdPlayer, sqDst] && pcSquares[sqDst] == 0))
                        continue;
                    sqDst += ccGuardDelta[j];
                    pcDst = pcSquares[sqDst];
                    if ((pcDst & pcSelfSide) == 0)
                        yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst);
                }
            }

            for (int i = GUARD_FROM; i <= GUARD_TO; i++)
            {
                sqSrc = sqPieces[pcSelfSide + i];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    sqDst = sqSrc + ccGuardDelta[j];
                    if (!IN_FORT[sqDst])
                    {
                        continue;
                    }
                    pcDst = pcSquares[sqDst];
                    if ((pcDst & pcSelfSide) == 0)
                        yield return new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst);
                }
            }

            sqSrc = sqPieces[pcSelfSide + KING_FROM];
            for (int i = 0; i < 4; i++)
            {
                sqDst = sqSrc + ccKingDelta[i];
                if (!IN_FORT[sqDst])
                    continue;
                pcDst = pcSquares[sqDst];
                if ((pcDst & pcSelfSide) == 0)
                    yield return new MOVE(sqSrc, sqDst, pcSelfSide + KING_FROM, pcDst);
            }
        }

        //List<MOVE> GenerateLegalMoves()
        //{
        //    int sqcheck = stepList[stepList.Count - 1].checking;
        //    List<MOVE> moves = GenerateMoves();
        //    foreach(MOVE m in moves)
        //    {
        //        if (sqcheck > 0)
        //        {
        //            int sqKing = sqPieces[SIDE_TAG(mySide) + KING_FROM];
        //            //如果被照将，先试试走棋后，照将着法是否仍然成立
        //            if (IsLegalMove(sqCheck, sqKing))
        //            {
        //                UndoMovePiece(m);
        //                return new Tuple<bool, int>(true, 0);
        //            }
        //        }
        //    }
        //    // 如果移动后被将军了，那么着法是非法的
        //    if (CheckedBy(mySide) > 0)
        //    {
        //        UndoMovePiece(m);
        //        return new Tuple<bool, int>(true, 0);
        //    }

        //}
    }

        //重量级的着法生成和排序函数
        //public IEnumerable<MOVE> Complex_GenMoves()
        //{
        //    //举例：当头炮与对方的帅之间隔了自己的马和对方的相，
        //    //自己的马就放在DiscoveredAttack里，对方的相就在PinnedPieces里
        //    List<int> discoveredAttack = new List<int>();
        //    int[] PinnedPieces = new int[48];   //0没有牵制，1纵向牵制，2横向牵制，3纵横牵制
        //    int sqSrc, sqDst, pcDst, delta;
        //    int[] oppAttackMap = new int[256];    //非行棋方保存攻击该格的价值最低的棋子
        //    captureMoves = new List<KeyValuePair<MOVE, int>>();
        //    List<KeyValuePair<MOVE, int>> nonCapMoves = new List<KeyValuePair<MOVE, int>>();

        //    //对阻挡将军的子进行判断
        //    void CheckBlocker(int side, int pcBlocker, int sqPinner, int direction)
        //    {
        //        int sdBlocker = SIDE(pcBlocker);
        //        int pcKind = cnPieceKinds[pcBlocker];
        //        //未过河兵没有牵制和闪击
        //        if (pcKind == PAWN && HOME_HALF[sdBlocker, sqPieces[pcBlocker]])
        //            return;
        //        if (sdBlocker == side)
        //        {
        //            //闪击
        //            if (sdBlocker == sdPlayer)
        //                discoveredAttack.Add(sdBlocker);
        //        }
        //        else
        //        {
        //            PinnedPieces[pcBlocker] |= direction;
        //            //在形如红炮-黑车-红兵-黑将的棋型中，黑车是可以吃红炮的
        //            if (IsLegalMove(sqPieces[pcBlocker], sqPinner))
        //            {
        //                if (sdBlocker == sdPlayer)
        //                    captureMoves.Add(new KeyValuePair<MOVE, int>(new MOVE(sqPieces[pcBlocker], sqPinner, pcBlocker, pcSquares[sqPinner]), 100));
        //                else
        //                    oppAttackMap[sqPinner] = pcBlocker;
        //            }

        //        }
        //    }

        //    int sd, bas, sqOppKing;
        //    //find absolute pin. 0没有牵制，1纵向牵制，2横向牵制，3纵横牵制
        //    for (sd = 0; sd < 2; sd++)
        //    {
        //        bas = SIDE_TAG(sd);
        //        sqOppKing = sqPieces[OPP_SIDE_TAG(sd) + KING_FROM];

        //        for (int pc = bas + ROOK_FROM; pc <= bas + ROOK_TO; pc++)
        //        {
        //            sqSrc = sqPieces[pc];
        //            if (SAME_FILE(sqSrc, sqOppKing))
        //            {
        //                delta = Math.Sign(sqOppKing - sqSrc) * 16;
        //                int pcBlocker = 0, nblock = 0;
        //                for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
        //                {
        //                    if (pcSquares[sq] != 0)
        //                    {
        //                        pcBlocker = pcSquares[sq];
        //                        nblock++;
        //                    }
        //                }
        //                if (nblock == 1)
        //                    CheckBlocker(sd, pcBlocker, sqSrc, 1);
        //            }

        //            if (SAME_RANK(sqSrc, sqOppKing))
        //            {
        //                delta = Math.Sign(sqOppKing - sqSrc);
        //                int pcBlocker = 0, nblock = 0;
        //                for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
        //                {
        //                    if (pcSquares[sq] != 0)
        //                    {
        //                        pcBlocker = pcSquares[sq];
        //                        nblock++;
        //                    }
        //                }
        //                if (nblock == 1)
        //                    CheckBlocker(sd, pcBlocker, sqSrc, 2);
        //            }
        //        }

        //        for (int pc = bas + CANNON_FROM; pc <= bas + CANNON_TO; pc++)
        //        {
        //            sqSrc = sqPieces[pc];
        //            if (SAME_FILE(sqSrc, sqOppKing))
        //            {
        //                delta = Math.Sign(sqOppKing - sqSrc) * 16;
        //                int nblock = 0;
        //                for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
        //                {
        //                    if (pcSquares[sq] != 0)
        //                        nblock++;
        //                }
        //                if (nblock == 2)
        //                    for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
        //                        CheckBlocker(sd, pcSquares[sq], sqSrc, 1);
        //            }
        //            if (SAME_RANK(sqSrc, sqOppKing))
        //            {
        //                delta = Math.Sign(sqOppKing - sqSrc);
        //                int nblock = 0;
        //                for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
        //                {
        //                    if (pcSquares[sq] != 0)
        //                        nblock++;
        //                }
        //                if (nblock == 2)
        //                    for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
        //                        CheckBlocker(sd, pcSquares[sq], sqSrc, 2);
        //            }
        //        }

        //        // 3. 判断对方的将是否被马威胁(以仕(士)的步长当作马腿)
        //        for (int i = 0; i < 4; i++)
        //        {
        //            int pcBlocker = pcSquares[sqOppKing + ccGuardDelta[i]];
        //            if (pcBlocker != 0)
        //                for (int j = 0; j < 2; j++)
        //                {
        //                    pcDst = pcSquares[sqOppKing + ccKnightCheckDelta[i, j]];
        //                    if (cnPieceTypes[pcDst] == bas + KNIGHT)
        //                        CheckBlocker(sd, pcBlocker, sqPieces[pcDst], 3);
        //                }
        //        }
        //    }

        //    //Generate enemy attack map, from most valuable piece to cheap piece
        //    sd = 1 - sdPlayer;
        //    bas = SIDE_TAG(sd);
        //    for (int pc = bas; pc < bas + 16; pc++)
        //    {
        //        sqSrc = sqPieces[pc];
        //        if (sqSrc == 0)
        //            continue;
        //        int pin = PinnedPieces[pc];

        //        switch (cnPieceKinds[pc])
        //        {
        //            case KING:
        //                for (int i = 0; i < 4; i++)
        //                {
        //                    sqDst = sqSrc + ccKingDelta[i];
        //                    if (IN_FORT[sqDst])
        //                        oppAttackMap[sqDst] = pc;
        //                }
        //                break;
        //            case ROOK:
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    if (ccPinDelta[pin, j])
        //                        continue;
        //                    delta = ccKingDelta[j];
        //                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
        //                    {
        //                        pcDst = pcSquares[sqDst];
        //                        oppAttackMap[sqDst] = pc;
        //                        if (pcDst != 0)
        //                            break;
        //                    }
        //                }
        //                break;
        //            case CANNON:
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    if (ccPinDelta[pin, j])
        //                        continue;
        //                    delta = ccKingDelta[j];
        //                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
        //                    {
        //                        if (pcSquares[sqDst] != 0) //炮架
        //                        {
        //                            for (sqDst += delta; IN_BOARD[sqDst]; sqDst += delta)
        //                            {
        //                                oppAttackMap[sqDst] = pc;
        //                                if (pcSquares[sqDst] != 0) //直瞄点
        //                                    goto NextFor;
        //                            }
        //                        }
        //                    }
        //                NextFor:;
        //                }
        //                //if (SAME_FILE(sqSrc, sqOppKing) || SAME_RANK(sqSrc, sqOppKing))
        //                //    positionValue[sd] += 5;
        //                break;
        //            case KNIGHT:
        //                if (pin > 0)
        //                    continue;
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    if (pcSquares[sqSrc + ccKingDelta[j]] == 0)
        //                    {
        //                        oppAttackMap[sqSrc + ccKnightDelta[j, 0]] = pc;
        //                        oppAttackMap[sqSrc + ccKnightDelta[j, 1]] = pc;
        //                    }
        //                }
        //                break;
        //            case PAWN:
        //                if ((pin & 1) == 0)
        //                    oppAttackMap[SQUARE_FORWARD(sqSrc, sd)] = pc;
        //                if ((pin & 2) == 0)
        //                    if (HOME_HALF[1 - sd, sqSrc])
        //                    {
        //                        oppAttackMap[sqSrc + 1] = pc;
        //                        oppAttackMap[sqSrc - 1] = pc;
        //                    }
        //                break;
        //            case BISHOP:
        //                if (pin > 0)
        //                    continue;
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    sqDst = sqSrc + ccGuardDelta[j];
        //                    if (HOME_HALF[sd, sqDst] && pcSquares[sqDst] == 0)
        //                        oppAttackMap[sqDst + ccGuardDelta[j]] = pc;
        //                }
        //                break;
        //            case GUARD:
        //                if (pin > 0)
        //                    continue;
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    sqDst = sqSrc + ccGuardDelta[j];
        //                    if (IN_FORT[sqDst])
        //                        oppAttackMap[sqDst] = pc;
        //                }
        //                break;
        //        }
        //    }

        //    void AddMove(int sqFrom, int sqTo)
        //    {
        //        int pcFrom = pcSquares[sqFrom];
        //        int pcTo = pcSquares[sqTo];
        //        if (SIDE(pcTo) == 1 - sdPlayer)
        //        {
        //            MOVE mv = new MOVE(sqFrom, sqTo, pcSquares[sqFrom], pcTo);
        //            int score = cnPieceValue[pcTo];
        //            if (oppAttackMap[sqTo] > 0)
        //                score -= cnPieceValue[pcFrom];
        //            captureMoves.Add(new KeyValuePair<MOVE, int>(mv, score));
        //        }
        //        else if (pcTo == 0)
        //        {
        //            MOVE mv = new MOVE(sqFrom, sqTo, pcSquares[sqFrom], pcTo);
        //            int score = oppAttackMap[sqTo] > 0 ? -cnPieceValue[pcFrom] : 0;
        //            nonCapMoves.Add(new KeyValuePair<MOVE, int>(mv, score));
        //        }
        //    }

        //    bas = SIDE_TAG(sdPlayer);
        //    //generate move list and assign score
        //    for (int pc = bas; pc < bas + 16; pc++)
        //    {
        //        sqSrc = sqPieces[pc];
        //        if (sqSrc == 0)
        //            continue;
        //        int pin = PinnedPieces[pc];

        //        switch (cnPieceKinds[pc])
        //        {
        //            case KING:
        //                for (int i = 0; i < 4; i++)
        //                {
        //                    sqDst = sqSrc + ccKingDelta[i];
        //                    if (IN_FORT[sqDst])
        //                        AddMove(sqSrc, sqDst);
        //                }
        //                break;
        //            case ROOK:
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    if (ccPinDelta[pin, j])
        //                        continue;
        //                    delta = ccKingDelta[j];
        //                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
        //                    {
        //                        pcDst = pcSquares[sqDst];
        //                        AddMove(sqSrc, sqDst);
        //                        if (pcDst != 0)
        //                            break;
        //                    }
        //                }
        //                break;
        //            case CANNON:
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    if (ccPinDelta[pin, j])
        //                        continue;
        //                    delta = ccKingDelta[j];
        //                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
        //                    {
        //                        pcDst = pcSquares[sqDst];
        //                        if (pcDst == 0)
        //                        {
        //                            int score = oppAttackMap[sqDst] > 0 ? 0 : -cnPieceValue[pcDst];
        //                            sqOppKing = sqPieces[OPP_SIDE_TAG(sdPlayer) + KING_FROM];
        //                            if (SAME_FILE(sqDst, sqOppKing) || SAME_RANK(sqDst, sqOppKing))
        //                                score += 5;
        //                            nonCapMoves.Add(new KeyValuePair<MOVE, int>(new MOVE(sqSrc, sqDst, pc, 0), history[sqSrc, sqDst]));
        //                        }
        //                        else
        //                        {
        //                            for (sqDst += delta; IN_BOARD[sqDst]; sqDst += delta)
        //                            {
        //                                pcDst = pcSquares[sqDst];
        //                                if (pcDst != 0)
        //                                {
        //                                    if ((pcDst & bas) == 0)
        //                                    {
        //                                        int score = cnPieceValue[pcDst];
        //                                        if (oppAttackMap[sqDst] > 0)
        //                                            score -= cnPieceValue[pc];
        //                                        captureMoves.Add(new KeyValuePair<MOVE, int>(new MOVE(sqSrc, sqDst, pc, pcDst), score));
        //                                    }
        //                                    goto NextFor1;
        //                                }
        //                            }

        //                        }
        //                    }
        //                NextFor1:;
        //                }
        //                break;
        //            case KNIGHT:
        //                if (pin > 0)
        //                    continue;
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    if (pcSquares[sqSrc + ccKingDelta[j]] == 0)
        //                    {
        //                        for (int k = 0; k < 2; k++)
        //                        {
        //                            sqDst = sqSrc + ccKnightDelta[j, k];
        //                            if (IN_BOARD[sqDst])
        //                                AddMove(sqSrc, sqDst);
        //                        }
        //                    }
        //                }
        //                break;
        //            case PAWN:
        //                if ((pin & 1) == 0)
        //                    AddMove(sqSrc, SQUARE_FORWARD(sqSrc, sd));
        //                if ((pin & 2) == 0)
        //                    if (HOME_HALF[1 - sd, sqSrc])
        //                    {
        //                        if (IN_BOARD[sqSrc + 1])
        //                            AddMove(sqSrc, sqSrc + 1);
        //                        if (IN_BOARD[sqSrc - 1])
        //                            AddMove(sqSrc, sqSrc - 1);
        //                    }
        //                break;
        //            case BISHOP:
        //                if (pin > 0)
        //                    continue;
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    sqDst = sqSrc + ccGuardDelta[j];
        //                    if (HOME_HALF[sd, sqDst] && pcSquares[sqDst] == 0)
        //                        AddMove(sqSrc, sqDst + ccGuardDelta[j]);
        //                }
        //                break;
        //            case GUARD:
        //                if (pin > 0)
        //                    continue;
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    sqDst = sqSrc + ccGuardDelta[j];
        //                    if (IN_FORT[sqDst])
        //                        AddMove(sqSrc, sqDst);
        //                }
        //                break;
        //        }
        //    }

        //    captureMoves.Sort(SortLarge2Small);
        //    foreach (KeyValuePair<MOVE, int> mv_vl in captureMoves)
        //        yield return mv_vl.Key;
        //    nonCapMoves.Sort(SortLarge2Small);
        //    foreach (KeyValuePair<MOVE, int> mv_vl in nonCapMoves)
        //        yield return mv_vl.Key;
        //}
    }
}