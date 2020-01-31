﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
/*
    我编了3个版本的审局函数，打算杀局时用简单的，静态局面时用复杂的。
    Middle_Evaluate是个不太成熟的评估函数，打算作废
 */

namespace MoleXiangqi
{
    partial class POSITION
    {
        //各种子力的价值
        const int MAT_KING = 0;
        const int MAT_ROOK = 130;
        const int MAT_CANNON = 55;
        const int MAT_KNIGHT = 65;
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
            15,  20,  28,  34,  40,
            15,  20,  25,  29,  34,
            14,  18,  23,  25,  29,
            10,  14,  19,  22,  25,
            5,   0,  15,  0,   15,
            0,   0,   5,   0,   10,
            0,   0,   0,   0,   0,
            0,   0,   0,   5,   2,
            0,   0,   0,   11,  15,
        };

            int[] cKnightHalfValue = {
             6,   3,  5,  10,   6,
            12,  19,  27,  24,  18,
            16,  17,  24,  27,  27,
            13,  18,  21,  24,  27,
            11, 15,  18,  21,  24,
            8,  12,  21,  18,  21,
            5,   9,  12,  15,  18,
            2,  6,  9,  12,  10,
            0,   3,  6,  8,  -20,
            -5,  0,  3,   0, -10};

            int[] cBishopGuardHalfValue = {
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,  -3,   0,   0,
             0,   0,   0,   0,   0,
             -5,   0,  0,  -3,  10,
             0,   0,   0,   0,  10,
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
                for (int i = bas; i < bas + 16; i++)
                {
                    int sq = sqPieces[i];
                    if (sq > 0)
                    {
                        int pcKind = cnPieceKinds[i];
                        int sqMirror = sd == 0 ? sq : SQUARE_FLIP(sq);
                        totalPieces++;
                        nP[sd, pcKind]++;
                        materialValue[sd] += cnPieceValue[pcKind];
                        switch (pcKind)
                        {
                            case PIECE_ROOK:
                                if (SAME_FILE(sq, sqOppKing) || SAME_RANK(sq, sqOppKing))
                                    positionValue[sd] += 15;
                                break;
                            case PIECE_CANNON:
                                if (SAME_FILE(sq, sqOppKing))
                                    positionValue[sd] += 20;
                                else if (SAME_RANK(sq, sqOppKing))
                                    positionValue[sd] += 12;
                                break;
                            case PIECE_KNIGHT:
                                positionValue[sd] += cKnightValue[sqMirror];
                                //检查绊马腿
                                for (int j = 0; j < 4; j++)
                                {
                                    int sqPin = sq + ccKingDelta[j];
                                    if (pcSquares[sqPin] != 0)
                                        positionValue[sd] -= 8;
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
            int[] pair = new int[2];
            for (int sd = 0; sd < 2; sd++)
            {
                //棋盘上的子越多，炮的威力越大，马的威力越小
                pair[sd] += (int)(2.4 * totalPieces * nP[sd, PIECE_CANNON]);  //可调系数
                pair[sd] -= (int)(0.9 * totalPieces * nP[sd, PIECE_KNIGHT]);  //可调系数
                //兵卒的价值随着对方攻击子力的减少而增加（即增加了过河的可能性）
                int enemyAttack = nP[1 - sd, PIECE_ROOK] * 2 + nP[1 - sd, PIECE_CANNON] + nP[1 - sd, PIECE_KNIGHT];
                int[] additionalPawnValue = { 28, 21, 15, 10, 6, 3, 2, 1, 0 };
                pair[sd] += nP[sd, PIECE_PAWN] * additionalPawnValue[enemyAttack];
                //兵种不全扣分
                if (nP[sd, PIECE_ROOK] == 0)
                    pair[sd] -= 30; //有车胜无车
                if (nP[sd, PIECE_CANNON] == 0)
                    pair[sd] -= 20;
                if (nP[sd, PIECE_KNIGHT] == 0)
                    pair[sd] -= 20;
                //缺相怕炮
                pair[sd] += (nP[sd, PIECE_BISHOP] - nP[1 - sd, PIECE_CANNON]) * 15;
                //缺仕怕车
                pair[sd] += (nP[sd, PIECE_GUARD] - nP[1 - sd, PIECE_ROOK]) * 15;
            }
            int scoreRed = materialValue[0] + positionValue[0] + pair[0];
            int scoreBlack = materialValue[1] + positionValue[1] + pair[1];

            int total = scoreRed - scoreBlack;
            ivpc[nStep, 0] = nStep;
            ivpc[nStep, 1] = total;
            ivpc[nStep, 2] = scoreRed;
            ivpc[nStep, 3] = scoreBlack;
            ivpc[nStep, 4] = materialValue[0];
            ivpc[nStep, 5] = materialValue[1];
            ivpc[nStep, 6] = positionValue[0];
            ivpc[nStep, 7] = positionValue[1];
            ivpc[nStep, 10] = pair[0];
            ivpc[nStep, 11] = pair[1];

            return total;
        }

        public int[,] ivpc; //统计每一步各个棋子的位置分 300 * 48
        public int Middle_Evaluate()
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
                        int sqDst, pcDst, sdDst;
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
                                        sdDst = pcSquares[sqDst] & 0b00110000;
                                        if (sdDst == 0)
                                            positionValue[sd] += 2; //车控制空白区域
                                        else if (sdDst == bas)
                                        {
                                            positionValue[sd] += 3; //车保护己方棋子
                                            break;
                                        }
                                        else
                                        {
                                            positionValue[sd] += 4; //车攻击对方棋子
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
                                                positionValue[sd] += (sqDst - sqSrc) / nDelta * 4;
                                            break;
                                        }
                                    }
                                    for (sqDst += nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                                    {
                                        pcDst = pcSquares[sqDst];
                                        if (pcDst != 0)
                                        {
                                            if (SIDE(pcDst) != sd)
                                                positionValue[sd] += 5; //炮直瞄对方棋子
                                            else
                                                positionValue[sd] += 3;  //炮保护己方棋子
                                            for (sqDst += nDelta; IN_BOARD[sqDst]; sqDst += nDelta)
                                            {
                                                pcDst = pcSquares[sqDst];
                                                if (pcDst != 0)
                                                {
                                                    if (SIDE(pcDst) == 1 - sd)
                                                    {
                                                        positionValue[sd] += 3; //炮间接瞄准位置
                                                        if (sqDst == sqOppKing)
                                                            positionValue[sd] += 15;
                                                    }
                                                    break;
                                                }
                                            }
                                            break;
                                        }
                                        else
                                            positionValue[sd] += 1;  //炮瞄准的空白区域
                                    }
                                }
                                break;
                            case PIECE_KNIGHT:
                                //马的机动性
                                for (int i = 0; i < 4; i++)
                                {
                                    int sqPin = sqSrc + ccKingDelta[i];
                                    if (pcSquares[sqPin] == 0)
                                    {
                                        for (int j = 0; j < 2; j++)
                                        {
                                            sqDst = sqSrc + ccKnightDelta[i, j];
                                            if (IN_BOARD[sqDst])
                                            {
                                                sdDst = pcSquares[sqDst] & 0b00110000;
                                                if (sdDst == 0)
                                                    positionValue[sd] += 2;
                                                else if (sdDst == sd)
                                                    positionValue[sd] += 3;
                                                else
                                                    positionValue[sd] += 5;
                                            }
                                        }
                                    }
                                }
                                //到王的距离 King Tropism
                                positionValue[sd] += (13 - Math.Abs(FILE_X(sqSrc) - FILE_X(sqOppKing)) - Math.Abs(RANK_Y(sqSrc) - RANK_Y(sqOppKing))) * 3;
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
                                    if ((HOME_HALF[sd, sqDst] && pcSquares[sqDst] == 0))
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
                        ivpc[nStep, pc - 14] = positionValue[sd] - posv0;
                    }
                }
            }
            ivpc[nStep, 0] = materialValue[0];
            ivpc[nStep, 1] = materialValue[1];
            return materialValue[0] - materialValue[1] + positionValue[0] - positionValue[1];
        }

        public int[,] attackMap;
        public int Complex_Evaluate()
        {
            //举例：当头炮与对方的帅之间隔了自己的马和对方的相，
            //自己的马就放在DiscoveredAttack里，对方的相就在PinnedPieces里
            int[] tacticValue = new int[2];
            bool[] PinnedPieces = new bool[48];
            bool[,] BannedGrids = new bool[2, 256];
            int sqSrc, sqDst, pcDst, delta;
            //对阻挡将军的子进行判断
            void CheckBlocker(int side, int blocker)
            {
                int blockerSide = SIDE(blocker);
                int pcKind = cnPieceKinds[blocker];
                //未过河兵没有牵制和闪击
                if (pcKind == PIECE_PAWN && HOME_HALF[blockerSide, sqPieces[blocker]])
                    return;
                if (blockerSide == side)
                {
                    //闪击加分，根据兵种不同
                    int[] v = { 0, 25, 20, 20, 7, 1, 3, 3 };
                    tacticValue[side] += v[pcKind];
                }
                else
                    PinnedPieces[blocker] = true;

            }
            //find absolute pin.
            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                int sqOppKing = sqPieces[OPP_SIDE_TAG(sd) + KING_FROM];

                for (int pc = bas + ROOK_FROM; pc <= bas + ROOK_TO; pc++)
                {
                    sqSrc = sqPieces[pc];
                    if (SAME_FILE(sqSrc, sqOppKing))
                    {
                        delta = Math.Sign(sqOppKing - sqSrc) * 16;
                        int pcBlocker = 0, nblock = 0;
                        for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                        {
                            if (pcSquares[sq] != 0)
                            {
                                pcBlocker = pcSquares[sq];
                                nblock++;
                            }
                        }
                        if (nblock == 1)
                            CheckBlocker(sd, pcBlocker);
                    }

                    if (SAME_RANK(sqSrc, sqOppKing))
                    {
                        delta = Math.Sign(sqOppKing - sqSrc);
                        int pcBlocker = 0, nblock = 0;
                        for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                        {
                            if (pcSquares[sq] != 0)
                            {
                                pcBlocker = pcSquares[sq];
                                nblock++;
                            }
                        }
                        if (nblock == 1)
                            CheckBlocker(sd, pcBlocker);
                    }
                }

                for (int pc = bas + CANNON_FROM; pc <= bas + CANNON_TO; pc++)
                {
                    sqSrc = sqPieces[pc];
                    if (SAME_FILE(sqSrc, sqOppKing))
                    {
                        delta = Math.Sign(sqOppKing - sqSrc) * 16;
                        int nblock = 0;
                        for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                        {
                            if (pcSquares[sq] != 0)
                                nblock++;
                        }
                        if (nblock == 2)
                            for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                                CheckBlocker(sd, pcSquares[sq]);
                        else if (nblock == 0) //空心炮
                        {
                            for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                                BannedGrids[1 - sd, sq] = true;
                        }
                    }
                    if (SAME_RANK(sqSrc, sqOppKing))
                    {
                        delta = Math.Sign(sqOppKing - sqSrc);
                        int nblock = 0;
                        for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                        {
                            if (pcSquares[sq] != 0)
                                nblock++;
                        }
                        if (nblock == 2)
                            for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                                CheckBlocker(sd, pcSquares[sq]);
                        else if (nblock == 0) //空心炮
                        {
                            for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                                BannedGrids[1 - sd, sq] = true;
                        }
                    }
                }

                // 3. 判断对方的将是否被马威胁(以仕(士)的步长当作马腿)
                for (int i = 0; i < 4; i++)
                {
                    int pcBlocker = pcSquares[sqOppKing + ccGuardDelta[i]];
                    if (pcBlocker != 0)
                        for (int j = 0; j < 2; j++)
                        {
                            pcDst = pcSquares[sqOppKing + ccKnightCheckDelta[i, j]];
                            if (cnPieceTypes[pcDst] == bas + PIECE_KNIGHT)
                                CheckBlocker(sd, pcBlocker);
                        }
                }
            }


            int totalPieces = 0;
            int[,] nP = new int[2, 8];  //每个兵种的棋子数量
            int[] materialValue = new int[2];
            int[] positionValue = new int[2];
            attackMap = new int[2, 256];

            //Generate attack map
            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                for (int pc = bas; pc < bas + 16; pc++)
                {
                    sqSrc = sqPieces[pc];
                    if (sqSrc == 0)
                        continue;
                    int pcKind = cnPieceKinds[pc];
                    int sqSrcMirror = sd == 0 ? sqSrc : SQUARE_FLIP(sqSrc);

                    totalPieces++;
                    nP[sd, pcKind]++;
                    materialValue[sd] += cnPieceValue[pcKind];
                    //对于pinned的棋子，不考虑其攻击力。
                    //这是一种简化的计算，实际上pin是有方向的，但这样太过复杂
                    if (PinnedPieces[pc])
                    {
                        ivpc[nStep, pc] = 0;
                        continue;
                    }
                    int posv0 = positionValue[sd];
                    switch (pcKind)
                    {
                        case PIECE_ROOK:
                            for (int j = 0; j < 4; j++)
                            {
                                delta = ccKingDelta[j];
                                for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                                {
                                    pcDst = pcSquares[sqDst];
                                    attackMap[sd, sqDst]++;
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
                                            attackMap[sd, sqDst]++;
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
                                    attackMap[sd, sqSrc + ccKnightDelta[j, 0]]++;
                                    attackMap[sd, sqSrc + ccKnightDelta[j, 1]]++;
                                }
                            }
                            //positionValue[sd] += cKnightValue[sqSrcMirror];
                            break;
                        case PIECE_PAWN:
                            sqDst = SQUARE_FORWARD(sqSrc, sd);
                            attackMap[sd, sqDst]++;
                            if (HOME_HALF[1 - sd, sqSrc])
                            {
                                attackMap[sd, sqSrc + 1]++;
                                attackMap[sd, sqSrc - 1]++;
                            }
                            positionValue[sd] += cKingPawnValue[sqSrcMirror];
                            break;
                        case PIECE_BISHOP:
                            for (int j = 0; j < 4; j++)
                            {
                                sqDst = sqSrc + ccGuardDelta[j];
                                if (HOME_HALF[sd, sqDst] && pcSquares[sqDst] == 0)
                                    attackMap[sd, sqDst + ccGuardDelta[j]]++;
                            }
                            //positionValue[sd] += cBishopGuardValue[sqSrcMirror];
                            break;
                        case PIECE_GUARD:
                            for (int j = 0; j < 4; j++)
                            {
                                sqDst = sqSrc + ccGuardDelta[j];
                                if (IN_FORT[sqDst])
                                    attackMap[sd, sqDst]++;
                            }
                            //positionValue[sd] += cBishopGuardValue[sqSrcMirror];
                            break;
                        case PIECE_KING:
                            for (int i = 0; i < 4; i++)
                            {
                                sqDst = sqSrc + ccKingDelta[i];
                                if (IN_FORT[sqDst])
                                    attackMap[sd, sqDst]++;
                            }
                            positionValue[sd] += cKingPawnValue[sqSrcMirror];
                            break;
                        default:
                            Debug.Fail("Unknown piece type");
                            break;
                    }
                    ivpc[nStep, pc] = positionValue[sd] - posv0;
                }
                //帅所在的格没有保护
                attackMap[sd, sqPieces[bas + KING_FROM]] = 0;
            }

            int[] pair = new int[2];
            for (int sd = 0; sd < 2; sd++)
            {
                //棋盘上的子越多，炮的威力越大，马的威力越小
                pair[sd] += (int)(2.4 * totalPieces * nP[sd, PIECE_CANNON]);  //可调系数
                pair[sd] -= (int)(0.9 * totalPieces * nP[sd, PIECE_KNIGHT]);  //可调系数
                //兵卒的价值随着对方攻击子力的减少而增加（即增加了过河的可能性）
                int enemyAttack = nP[1 - sd, PIECE_ROOK] * 2 + nP[1 - sd, PIECE_CANNON] + nP[1 - sd, PIECE_KNIGHT];
                int[] additionalPawnValue = { 28, 21, 15, 10, 6, 3, 2, 1, 0 };
                pair[sd] += nP[sd, PIECE_PAWN] * additionalPawnValue[enemyAttack];
                //兵种不全扣分
                if (nP[sd, PIECE_ROOK] == 0)
                    pair[sd] -= 30; //有车胜无车
                if (nP[sd, PIECE_CANNON] == 0)
                    pair[sd] -= 20;
                if (nP[sd, PIECE_KNIGHT] == 0)
                    pair[sd] -= 20;
                //缺相怕炮
                pair[sd] += (nP[sd, PIECE_BISHOP] - nP[1 - sd, PIECE_CANNON]) * 15;
                //缺仕怕车
                pair[sd] += (nP[sd, PIECE_GUARD] - nP[1 - sd, PIECE_ROOK]) * 15;
            }

            int[] connectivity = new int[2];
            for (int y = RANK_TOP; y <= RANK_BOTTOM; y++)
                for (int x = FILE_LEFT; x <= FILE_RIGHT; x++)
                {
                    int sq = XY2Coord(x, y);
                    int pc = pcSquares[sq];
                    int sd = SIDE(pc);
                    if (sd != -1)
                    {
                        //攻击有根子分数4，攻击无根子分数8
                        if (attackMap[sd, sq] > 0)
                        {
                            connectivity[sd] += 2; //受保护分数
                            connectivity[1 - sd] += attackMap[1 - sd, sq] * 4;
                        }
                        else
                            connectivity[1 - sd] += attackMap[1 - sd, sq] * 8;
                    }
                    else
                    {
                        for (int i = 0; i < 2; i++)
                            if (BannedGrids[i, sq])
                                connectivity[1 ^ i] += attackMap[1 ^ i, sq] * 3;
                            else    //机动性，重复计算不超过2
                                connectivity[i] += Math.Min(attackMap[i, sq], 2);
                    }
                }
            if (nStep % 2 == 0 && attackMap[1, sqPieces[16 + KING_FROM]] > 0)
            {
                Console.WriteLine("{0}. Red in check.", nStep);
                connectivity[1] += 10;
            }
            if (nStep % 2 == 1 && attackMap[0, sqPieces[32 + KING_FROM]] > 0)
            {
                Console.WriteLine("{0}. Black in check.", nStep);
                connectivity[0] += 10;
            }

            int scoreRed = materialValue[0] + positionValue[0] + pair[0] + connectivity[0] + tacticValue[0];
            int scoreBlack = materialValue[1] + positionValue[1] + pair[1] + connectivity[1] + tacticValue[1];

            int total = scoreRed - scoreBlack;
            ivpc[nStep, 0] = nStep;
            ivpc[nStep, 1] = total;
            ivpc[nStep, 2] = scoreRed;
            ivpc[nStep, 3] = scoreBlack;
            ivpc[nStep, 4] = materialValue[0];
            ivpc[nStep, 5] = materialValue[1];
            ivpc[nStep, 6] = positionValue[0];
            ivpc[nStep, 7] = positionValue[1];
            ivpc[nStep, 8] = connectivity[0];
            ivpc[nStep, 9] = connectivity[1];
            ivpc[nStep, 10] = pair[0];
            ivpc[nStep, 11] = pair[1];
            ivpc[nStep, 12] = tacticValue[0];
            ivpc[nStep, 13] = tacticValue[1];
            return total;
        }
    }
}
