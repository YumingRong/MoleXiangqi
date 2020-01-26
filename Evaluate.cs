using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    partial class POSITION
    {
        //各种子力的价值
        const int MAT_KING = 2000;
        const int MAT_ROOK = 140;
        const int MAT_CANNON = 50;
        const int MAT_KNIGHT = 69;
        const int MAT_PAWN = 10;
        const int MAT_BISHOP = 25;
        const int MAT_ADVISOR = 20;

        static readonly int[] cnPieceValue = { 0, MAT_ROOK, MAT_CANNON, MAT_KNIGHT, MAT_PAWN, MAT_KING, MAT_BISHOP, MAT_ADVISOR };

        int[] cKingPawnValue;
        int[] cKnightValue;
        int[] cBishopGuardValue;

        void InitEval()
        {
            //只列出左半边位置分数数组，以方便修改
            int[] cKingPawnHalfValue = {
            1,   3,   5,   7,   9,
            15,  20,  34,  42,  60,
            23,  27,  32,  37,  40,
            22,  25,  27,  30,  35,
            20,  22,  25,  27,  28,
            10,   0,  15,  0,   15,
            5,   0,   5,   0,   10,
            0,   0,   0,   0,   0,
            0,   0,   0,   5,   2,
            0,   0,   0,   11,  15,
        };

            int[] cKnightHalfValue = {
             5,   0,  10,  10,   0,
            12,  19,  40,  20,  33,
            20,  26,  39,  45,  45,
            13,  35,  33,  37,  40,
            11,  25,  30,  35,  35,
            10,  25,  30,  30,  35,
            5,   10,  20,  20,  25,
            10,  15,  20,  20,  10,
            -5,   5,  10,  5,  -20,
            -10,  0,  5,   -5, -10};

            int[] cBishopGuardHalfValue = {
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0, -10,   0,   0,
             0,   0,   0,   0,   0,
             -5,   0,  0,  -5,  10,
             0,   0,   0,   0,  12,
             0,   0,   0,   0,   0};


            int[] InitEvalArray(int[] origin)
            {
                int[] pos = new int[256];
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 4; x++)
                        pos[iXY2Coord(x, y)] = pos[iXY2Coord(8 - x, y)] = origin[x + y * 5];
                    pos[iXY2Coord(4, y)] = origin[4 + y * 5];
                }
                return pos;
            }
            cKingPawnValue = InitEvalArray(cKingPawnHalfValue);
            cKnightValue = InitEvalArray(cKnightHalfValue);
            cBishopGuardValue = InitEvalArray(cBishopGuardHalfValue);
        }

        public int Simple_Evaluate()
        {
            int totalPieces = 0;
            int[,] nP = new int[2, 8];
            int[] materialValue = new int[2];
            int[] positionValue = new int[2];
            materialValue[0] = materialValue[1] = 0;

            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                int sqOppKing = sqPieces[OPP_SIDE_TAG(sd) + KING_FROM];
                int sqMirror, sqOppKingMirror;
                sqOppKingMirror = sd==0? sqOppKing: SQUARE_FLIP(sqOppKing);
                for (int i = bas; i < bas + 16; i++)
                {
                    int sq = sqPieces[i];
                    if (sq > 0)
                    {
                        int pcKind = cnPieceKinds[i];
                        sqMirror = sd==0? sq: SQUARE_FLIP(sq);
                        totalPieces++;
                        nP[sd, pcKind]++;
                        materialValue[sd] += cnPieceValue[pcKind];
                        switch (pcKind)
                        {
                            case PIECE_ROOK:
                                if (FILE_X(sq) == FILE_X(sqOppKing) || RANK_Y(sq) == RANK_Y(sqOppKing))
                                    positionValue[sd] += 20;
                                break;
                            case PIECE_CANNON:
                                if (FILE_X(sq) == FILE_X(sqOppKing))
                                    positionValue[sd] += 40;
                                else if (RANK_Y(sq) == RANK_Y(sqOppKing))
                                    positionValue[sd] += 20;
                                break;
                            case PIECE_KNIGHT:
                                positionValue[sd] += cKnightValue[sqMirror];
                                //检查绊马腿
                                for (int j = 0; j < 4; j++)
                                {
                                    int sqPin = sq + ccKingDelta[j];
                                    if (IN_BOARD[sqPin] && pcSquares[sqPin] == 0)
                                    {
                                        int sqDst = sq + ccKnightDelta[j, 0];
                                        if (IN_BOARD[sqDst])
                                            positionValue[sd] += 5;
                                        sqDst = sq + ccKnightDelta[j, 1];
                                        if (IN_BOARD[sqDst])
                                            positionValue[sd] += 5;
                                    }
                                }
                                break;
                            case PIECE_PAWN:
                            case PIECE_KING:
                                positionValue[sd] += cKingPawnValue[sqMirror];
                                break;
                            case PIECE_BISHOP:
                            case PIECE_GUARD:
                                positionValue[sd] += cBishopGuardValue[sqMirror];
                                break;
                        }
                    }
                }
            }

            for (int sd = 0; sd < 2; sd++)
            {
                //棋盘上的子越多，炮的威力越大，马的威力越小
                materialValue[sd] += (int)(2.4 * totalPieces * nP[sd, PIECE_CANNON]);  //可调系数
                materialValue[sd] -= (int)(0.9 * totalPieces * nP[sd, PIECE_KNIGHT]);  //可调系数
            }

            return materialValue[0] - materialValue[1] + positionValue[0] - positionValue[1];
        }

        public int[,] vpc = new int[300, 34]; //统计每一步各个棋子的位置分
        public int Complex_Evaluate()
        {
            int totalPieces = 0;
            int[,] nP = new int[2, 8];
            int[] materialValue = new int[2];
            int[] positionValue = new int[2];
            materialValue[0] = materialValue[1] = 0;

            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                int sqOppKing = sqPieces[OPP_SIDE_TAG(sd) + KING_FROM];
                for (int pc = bas; pc < bas + 16; pc++)
                {
                    int sqSrc = sqPieces[pc];
                    if (sqSrc > 0)
                    {
                        int pcKind = cnPieceKinds[pc];
                        Debug.Assert(IN_BOARD[sqSrc]);
                        totalPieces++;
                        nP[sd, pcKind]++;
                        materialValue[sd] += cnPieceValue[pcKind];
                        int sqDst, pcDst;
                        int posv0 = positionValue[sd];
                        switch (pcKind)
                        {
                            //车的机动性
                            case PIECE_ROOK:
                                if (FILE_X(sqSrc) == FILE_X(sqOppKing) || RANK_Y(sqSrc) == RANK_Y(sqOppKing))
                                    positionValue[sd] += 20;
                                for (int i = 0; i < 4; i++)
                                {
                                    int nDelta = ccKingDelta[i];
                                    for (sqDst = sqSrc + nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                                    {
                                        pcDst = pcSquares[sqDst];
                                        if (pcDst == 0)
                                            positionValue[sd] += 2;
                                        else
                                        {
                                            positionValue[sd] += 2;
                                            break;
                                        }
                                    }
                                }
                                break;
                            case PIECE_CANNON:
                                for (int j = 0; j < 4; j++)
                                {
                                    int nDelta = ccKingDelta[j];
                                    for (sqDst = sqSrc + nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                                    {
                                        pcDst = pcSquares[sqDst];
                                        if (pcDst != 0)
                                        {
                                            if (sqDst == sqOppKing) //炮与将之间是禁区
                                                positionValue[sd] += (sqDst - sqSrc) / nDelta * 2;
                                            break;
                                        }
                                    }
                                    for (sqDst += nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                                    {
                                        pcDst = pcSquares[sqDst];
                                        if (pcDst != 0)
                                        {
                                            if (SIDE(pcDst) != sd)
                                                positionValue[sd] += 4;
                                            for (sqDst += nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                                            {
                                                pcDst = pcSquares[sqDst];
                                                if (pcDst != 0)
                                                {
                                                    if (SIDE(pcDst) == 1 - sd)
                                                    {
                                                        positionValue[sd] += 3;
                                                        if (sqDst == sqOppKing)
                                                            positionValue[sd] += 15;
                                                    }
                                                    break;
                                                }
                                                else
                                                    positionValue[sd] += 2;
                                            }
                                            break;
                                        }
                                    }
                                }
                                break;
                            case PIECE_KNIGHT:
                                //马的机动性
                                for (int i = 0; i < 4; i++)
                                {
                                    int sqPin = sqSrc + ccKingDelta[i];
                                    if (IN_BOARD[sqPin] && pcSquares[sqPin] == 0)
                                    {
                                        sqDst = sqSrc + ccKnightDelta[i, 0];
                                        if (IN_BOARD[sqDst])
                                            positionValue[sd] += 3;
                                        sqDst = sqSrc + ccKnightDelta[i, 1];
                                        if (IN_BOARD[sqDst])
                                            positionValue[sd] += 3;
                                    }
                                }
                                //到王的距离
                                positionValue[sd] += (13 - Math.Abs(FILE_X(sqSrc) - FILE_X(sqOppKing)) - Math.Abs(RANK_Y(sqSrc) - RANK_Y(sqOppKing)))*3;
                                break;
                            case PIECE_PAWN:
                                int sqMirror = sd == 0 ? sqSrc : SQUARE_FLIP(sqSrc);
                                positionValue[sd] += cKingPawnValue[sqMirror];
                                break;
                            case PIECE_KING:
                                sqMirror = sd == 0 ? sqSrc : SQUARE_FLIP(sqSrc);
                                positionValue[sd] += cKingPawnValue[sqMirror];
                                break;
                            case PIECE_BISHOP:
                                for (int i = 0; i < 4; i++)
                                {
                                    sqDst = sqSrc + ccGuardDelta[i];
                                    if ((IN_BOARD[sqDst] && HOME_HALF(sqDst, sd) && pcSquares[sqDst] == 0))
                                        positionValue[sd] += 4;
                                }
                                sqMirror = sd == 0 ? sqSrc : SQUARE_FLIP(sqSrc);
                                positionValue[sd] += cBishopGuardValue[sqMirror];
                                break;
                            case PIECE_GUARD:
                                sqMirror = sd == 0 ? sqSrc : SQUARE_FLIP(sqSrc);
                                positionValue[sd] += cBishopGuardValue[sqMirror];
                                break;
                        }
                        vpc[nStep, pc - 14] = positionValue[sd] - posv0;
                    }
                }
            }
            vpc[nStep, 0] = materialValue[0];
            vpc[nStep, 1] = materialValue[1];
            return materialValue[0] - materialValue[1] + positionValue[0] - positionValue[1];
        }
    }
}
