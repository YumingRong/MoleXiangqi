using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    public struct MOVE
    {
        public int sqSrc, sqDst;      // 起始格和目标格
        public int pcSrc, pcDst;

        public MOVE(int sqFrom, int sqTo, int pcFrom, int pcTo)
        {
            sqSrc = sqFrom;
            sqDst = sqTo;
            pcSrc = pcFrom;
            pcDst = pcTo;
        }
    }
    
    public partial class POSITION
    {
        // 帅(将)的步长
        static readonly int[] ccKingDelta = { -0x10, -0x01, +0x01, +0x10 };
        // 仕(士)的步长
        static readonly int[] ccGuardDelta = { -0x11, -0x0f, +0x0f, +0x11 };
        // 马的步长，以帅(将)的步长作为马腿
        static readonly int[,] ccKnightDelta = { { -33, -31 }, { -18, 14 }, { -14, 18 }, { 31, 33 } };
        // 马被将军的步长，以仕(士)的步长作为马腿
        static readonly int[,] ccKnightCheckDelta = { { -33, -18 }, { -31, -14 }, { 14, 31 }, { 18, 33 } };

        //着法生成器
        public List<MOVE> GenerateMoves()
        {
            int sqSrc, sqDst, pcDst;
            int pcSelfSide, pcOppSide;
            int nDelta;
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
                    nDelta = ccKingDelta[j];
                    for (sqDst = sqSrc + nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
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
                    nDelta = ccKingDelta[j];
                    for (sqDst = sqSrc + nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                    {
                        pcDst = pcSquares[sqDst];
                        if (pcDst == 0)
                            mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, 0));
                        else
                        {
                            for (sqDst += nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
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
                if (HOME_HALF[1- sdPlayer, sqSrc])
                {
                    for (nDelta = -1; nDelta <= 1; nDelta += 2)
                    {
                        sqDst = sqSrc + nDelta;
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

        List<MOVE> GenerateCaptures()
        {
            int sqSrc, sqDst, pcDst;
            int pcSelfSide, pcOppSide;
            int nDelta;
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
                    nDelta = ccKingDelta[j];
                    for (sqDst = sqSrc + nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                    {
                        pcDst = pcSquares[sqDst];
                        if (pcDst != 0)
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
                    nDelta = ccKingDelta[j];
                    for (sqDst = sqSrc + nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                    {
                        if (pcSquares[sqDst] != 0)
                        {
                            for (sqDst += nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                            {
                                pcDst = pcSquares[sqDst];
                                if (pcDst != 0)
                                {
                                    if ((pcDst & pcOppSide) != 0)
                                        mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                                    goto NextFor2;
                                }
                            }
                        }
                    }
                NextFor2:;
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
                        if (IN_BOARD[sqDst] && (pcDst & pcOppSide) != 0)
                            mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                        sqDst = sqSrc + ccKnightDelta[j, 1];
                        pcDst = pcSquares[sqDst];
                        if (IN_BOARD[sqDst] && (pcDst & pcOppSide) != 0)
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
                    if ((pcDst & pcOppSide) != 0)
                        mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
                }
                if (HOME_HALF[1- sdPlayer, sqSrc])
                {
                    for (nDelta = -1; nDelta <= 1; nDelta += 2)
                    {
                        sqDst = sqSrc + nDelta;
                        if (IN_BOARD[sqDst])
                        {
                            pcDst = pcSquares[sqDst];
                            if ((pcDst & pcOppSide) != 0)
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
                    if ((pcDst & pcOppSide) != 0)
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
                        continue;
                    pcDst = pcSquares[sqDst];
                    if ((pcDst & pcOppSide) != 0)
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
                if ((pcDst & pcOppSide) != 0)
                    mvs.Add(new MOVE(sqSrc, sqDst, pcSelfSide + i, pcDst));
            }
            return mvs;
        }

        // 着法合理性检测，仅用在“杀手着法”的检测中
        public bool LegalMove(int sqSrc, int sqDst)
        {
            int sqPin, pcMoved, pcCaptured, selfSide;
            int nDelta;
            // 着法合理性检测包括以下几个步骤：

            // 1. 检查要走的子是否存在
            selfSide = SIDE_TAG(sdPlayer);
            pcMoved = pcSquares[sqSrc];
            if ((pcMoved & selfSide) == 0)
            {
                return false;
            }
            if (!IN_BOARD[sqSrc] || !IN_BOARD[sqDst])
                return false;

            // 2. 检查吃到的子是否为对方棋子(如果有吃子的话)
            pcCaptured = pcSquares[sqDst];
            if ((pcCaptured & selfSide) != 0)
                return false;

            switch (cnPieceKinds[pcMoved])
            {
                // 3. 如果是帅(将)或仕(士)，则先看是否在九宫内，再看是否是合理位移
                case PIECE_KING:
                    return IN_FORT[sqDst] && KING_SPAN(sqSrc, sqDst);
                case PIECE_GUARD:
                    return IN_FORT[sqDst] && ADVISOR_SPAN(sqSrc, sqDst);

                // 4. 如果是相(象)，则先看是否过河，再看是否是合理位移，最后看有没有被塞象眼
                case PIECE_BISHOP:
                    return SAME_HALF(sqSrc, sqDst) && BISHOP_SPAN(sqSrc, sqDst) && pcSquares[BISHOP_PIN(sqSrc, sqDst)] == 0;

                // 5. 如果是马，则先看看是否是合理位移，再看有没有被蹩马腿
                case PIECE_KNIGHT:
                    sqPin = KNIGHT_PIN(sqSrc, sqDst);
                    return sqPin != sqSrc && pcSquares[sqPin] == 0;

                // 6. 如果是车，则先看是横向移动还是纵向移动
                case PIECE_ROOK:
                    if (SAME_RANK(sqSrc, sqDst))
                    {
                        nDelta = (sqDst < sqSrc ? -1 : 1);
                    }
                    else if (SAME_FILE(sqSrc, sqDst))
                    {
                        nDelta = (sqDst < sqSrc ? -16 : 16);
                    }
                    else
                    {
                        return false;
                    }
                    for (sqPin = sqSrc + nDelta; sqPin != sqDst; sqPin += nDelta)
                    {
                        if (pcSquares[sqPin] > 0)
                            return false;
                    }
                    return true;

                // 7. 如果是炮，判断起来和车一样
                case PIECE_CANNON:
                    if (SAME_RANK(sqSrc, sqDst))
                        nDelta = (sqDst < sqSrc ? -1 : 1);
                    else if (SAME_FILE(sqSrc, sqDst))
                        nDelta = (sqDst < sqSrc ? -16 : 16);
                    else
                        return false;
                    int nPin = 0;
                    for (sqPin = sqSrc + nDelta; sqPin != sqDst; sqPin += nDelta)
                    {
                        if (pcSquares[sqPin] > 0)
                            nPin++;
                    }
                    return pcCaptured > 0 && nPin == 1 || pcCaptured == 0 && nPin == 0;

                // 8. 如果是兵(卒)，则按红方和黑方分情况讨论
                default:
                    if( sqDst == SQUARE_FORWARD(sqSrc, sdPlayer))
                        return true;
                    if (HOME_HALF[1 - sdPlayer, sqSrc] && (sqDst == sqSrc - 1 || sqDst == sqSrc + 1))
                        return true;
                    else
                        return false;
            }
        }

        // 判断是否被将军
        public bool Checked(int side)
        {
            int i, j, sqSrc, sqDst;
            int pcSelfSide, pcOppSide, pcDst, nDelta;
            pcSelfSide = SIDE_TAG(side);
            pcOppSide = OPP_SIDE_TAG(side);
            // 找到棋盘上的帅(将)，再做以下判断：
            sqSrc = sqPieces[pcSelfSide + KING_FROM];
            Debug.Assert(IN_BOARD[sqSrc]);

            // 1. 判断是否被对方的兵(卒)将军
            if (cnPieceTypes[pcSquares[SQUARE_FORWARD(sqSrc, side)]] == pcOppSide + PIECE_PAWN)
                return true;
            if (cnPieceTypes[pcSquares[sqSrc - 1]] == pcOppSide + PIECE_PAWN)
                return true;
            if (cnPieceTypes[pcSquares[sqSrc + 1]] == pcOppSide + PIECE_PAWN)
                return true;

            // 2. 判断是否被对方的马将军(以仕(士)的步长当作马腿)
            for (i = 0; i < 4; i++)
            {
                int sqPin = sqSrc + ccGuardDelta[i];
                if (pcSquares[sqPin] == 0)
                    for (j = 0; j < 2; j++)
                    {
                        pcDst = pcSquares[sqSrc + ccKnightCheckDelta[i, j]];
                        if (cnPieceTypes[pcDst] == pcOppSide + PIECE_KNIGHT)
                            return true;
                    }
            }

            // 3. 判断是否被对方的车或炮将军
            for (i = 0; i < 4; i++)
            {
                nDelta = ccKingDelta[i];
                for (sqDst = sqSrc + nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                {
                    pcDst = pcSquares[sqDst];
                    if (pcDst != 0)
                    {
                        pcDst = cnPieceTypes[pcDst];
                        if (pcDst == pcOppSide + PIECE_ROOK)
                            return true;
                        else
                            for (sqDst += nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                            {
                                pcDst = pcSquares[sqDst];
                                if (pcDst != 0)
                                {
                                    if (cnPieceTypes[pcDst] == pcOppSide + PIECE_CANNON)
                                        return true;
                                    goto NextFor3;
                                }
                            }
                    }
                }
            NextFor3:;
            }

            //4. 判断是否将帅对脸
            sqSrc = sqPieces[32 + KING_FROM];
            sqDst = sqPieces[16 + KING_FROM];
            if (SAME_FILE(sqSrc, sqDst))
            {
                for (i = sqSrc + 16; i < sqDst; i += 16)
                    if (pcSquares[i] > 0)
                        return false;
            }
            else
                return false;
            return true;
        }

        // 判断是否被杀
        public bool IsMate(int side)
        {
            List<MOVE> mvs;

            mvs = GenerateMoves();
            foreach (MOVE mv in mvs)
            {
                //Debug.WriteLine(iMove2Coord(mv) + "," + SRC(mv) + "-" + DST(mv));
                MovePiece(mv);
                if (!Checked(side))
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

    }
}