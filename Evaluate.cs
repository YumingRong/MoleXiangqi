using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MathNet.Numerics.Statistics;

namespace MoleXiangqi
{
    partial class POSITION
    {
        //各种子力的价值
        const int MAT_KING = 5000;
        const int MAT_ROOK = 135;
        const int MAT_CANNON = 65;
        const int MAT_KNIGHT = 60;
        const int MAT_PAWN = 10;
        const int MAT_BISHOP = 25;
        const int MAT_ADVISOR = 20;

        static readonly int[] cnPieceValue =
            { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            MAT_KING, MAT_ROOK, MAT_ROOK, MAT_CANNON, MAT_CANNON, MAT_KNIGHT, MAT_KNIGHT, MAT_PAWN, MAT_PAWN, MAT_PAWN, MAT_PAWN, MAT_PAWN, MAT_BISHOP, MAT_BISHOP, MAT_ADVISOR, MAT_ADVISOR,
            MAT_KING, MAT_ROOK, MAT_ROOK, MAT_CANNON, MAT_CANNON, MAT_KNIGHT, MAT_KNIGHT, MAT_PAWN, MAT_PAWN, MAT_PAWN, MAT_PAWN, MAT_PAWN, MAT_BISHOP, MAT_BISHOP, MAT_ADVISOR, MAT_ADVISOR};

        internal int[] cKingPawnValue, cRookValue, cKnightValue, cBishopGuardValue;

        void InitEval()
        {
            //只列出左半边位置分数数组，以方便修改
            int[] cRookHalfValue = {
            10,  13,  13,  23,  22,
            8,   15,  15,  23,  25,
            8,   15,  13,  20,  22,
            13,  18,  23,  23,  22,
            17,  17,  19,  22,  22,
            15,  18,  15,  18,  15,
            8,   15,  15,  16,  14,
            0,   12,  10,  15,  10,
            7,   12,   8,  13,   2,
            0,   10,   8,  14, -10,
        };

            int[] cKingPawnHalfValue = {
            1,   3,   5,   7,   9,
            15,  20,  28,  34,  40,
            15,  20,  25,  29,  34,
            14,  18,  23,  25,  29,
            10,  14,  19,  22,  25,
            3,   0,  14,  0,   15,
            0,   0,   5,   0,   10,
            0,   0,   0,   0,   0,
            0,   0,   0,   5,   2,
            0,   0,   0,   11,  15,
        };

            int[] cKnightHalfValue = {
            10,   7,  5,   10,   6,
            12,  16,  24,  20,  18,
            16,  17,  23,  20,  27,
            13,  20,  21,  24,  23,
            11,  20,  18,  20,  24,
             8,  14,  20,  17,  24,
             5,   9,  12,  15,  17,
             7,   6,   9,  12,  10,
             0,   3,   6,   8, -15,
            -5,   0,   3,   0, -10};

            int[] cBishopGuardHalfValue = {
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,  -3,   0,   0,
             0,   0,   0,   0,   0,
             -5,   0,  0,  -3,   7,
             0,   0,   0,   0,   5,
             0,   0,   0,   0,   0};


            int[] InitEvalArray(int[] origin)
            {
                int[] pos = new int[256];
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 4; x++)
                        pos[UI_XY2Coord(x, y)] = pos[UI_XY2Coord(8 - x, y)] = origin[x + y * 5];
                    pos[UI_XY2Coord(4, y)] = origin[4 + y * 5];
                }
                return pos;
            }
            cKingPawnValue = InitEvalArray(cKingPawnHalfValue);
            cKnightValue = InitEvalArray(cKnightHalfValue);
            cBishopGuardValue = InitEvalArray(cBishopGuardHalfValue);
            cRookValue = InitEvalArray(cRookHalfValue);
        }

        //以下数组都是Complex_Evaluate的输出
        public int[,] ivpc; //统计每一步各个棋子的位置分 300 * 48，供调试用
        public int[,] connectivityMap; //供统计调试用
        //public int[,] attackMap;
        /*Complex_evaluate可以顺便创建吃子走法并打分，虽然可能不全，比如两个子同时攻击同一个格子
         但是这种情况较少，且一般情况下总是优先用低价值的棋子去吃对方。
         而且调用captureMoves的静态搜素并不需要严格考虑所用局面。          */
        public List<KeyValuePair<MOVE, int>> captureMoves;
        public int Complex_Evaluate()
        {
            //举例：当头炮与对方的帅之间隔了自己的马和对方的相，
            //自己的马就放在DiscoveredAttack里，对方的相就在PinnedPieces里
            int[] tacticValue = new int[2];
            int[] PinnedPieces = new int[48];   //0没有牵制，1纵向牵制，2横向牵制，3纵横牵制
            bool[,] BannedGrids = new bool[2, 256]; //空头炮与将之间不能走子
            int sqSrc, sqDst, pcDst, delta;
            int[,] attackMap;

            int[] cDiscoveredAttack = { 0, 1, 25, 20, 20, 7, 3, 3 };
            //对阻挡将军的子进行判断
            void CheckBlocker(int side, int pcBlocker, int sqPinner, int direction)
            {
                int sdBlocker = SIDE(pcBlocker);
                int pcKind = cnPieceKinds[pcBlocker];
                //未过河兵没有牵制和闪击
                if (pcKind == PAWN && HOME_HALF[sdBlocker, sqPieces[pcBlocker]])
                    return;
                if (sdBlocker == side)
                {
                    //闪击加分，根据兵种不同
                    tacticValue[side] += cDiscoveredAttack[pcKind];
                }
                else
                {
                    PinnedPieces[pcBlocker] |= direction;
                    //在形如红炮-黑车-红兵-黑将的棋型中，黑车是可以吃红炮的
                    if (IsLegalMove(sqPieces[pcBlocker], sqPinner))
                        attackMap[sdBlocker, sqPinner] = pcBlocker;

                }
            }

            attackMap = new int[2, 256];    //保存攻击该格的价值最低的棋子
            //find absolute pin. 0没有牵制，1纵向牵制，2横向牵制，3纵横牵制
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
                            CheckBlocker(sd, pcBlocker, sqSrc, 1);
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
                            CheckBlocker(sd, pcBlocker, sqSrc, 2);
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
                        {
                            for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                                if (pcSquares[sq] > 0)
                                    CheckBlocker(sd, pcSquares[sq], sqSrc, 1);
                        }
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
                        {
                            for (int sq = sqSrc + delta; sq != sqOppKing; sq += delta)
                                if (pcSquares[sq] > 0)
                                    CheckBlocker(sd, pcSquares[sq], sqSrc, 2);
                        }
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
                            if (cnPieceTypes[pcDst] == bas + KNIGHT)
                                CheckBlocker(sd, pcBlocker, sqPieces[pcDst], 3);
                        }
                }
            }


            int totalPieces = 0;
            int[,] nP = new int[2, 8];  //每个兵种的棋子数量
            int[] materialValue = new int[2];
            int[] positionValue = new int[2];
            connectivityMap = new int[2, 256];

            //Generate attack map, from most valuable piece to cheap piece
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
                    int pin = PinnedPieces[pc];
                    totalPieces++;
                    nP[sd, pcKind]++;
                    materialValue[sd] += cnPieceValue[pc];

                    //int posv0 = positionValue[sd];
                    switch (pcKind)
                    {
                        case KING:
                            for (int i = 0; i < 4; i++)
                            {
                                sqDst = sqSrc + ccKingDelta[i];
                                if (IN_FORT[sqDst])
                                    attackMap[sd, sqDst] = pc;
                            }
                            positionValue[sd] += cKingPawnValue[sqSrcMirror];
                            break;
                        case ROOK:
                            for (int j = 0; j < 4; j++)
                            {
                                if (ccPinDelta[pin, j])
                                    continue;
                                delta = ccKingDelta[j];
                                for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                                {
                                    pcDst = pcSquares[sqDst];
                                    attackMap[sd, sqDst] = pc;
                                    if (pcDst != 0)
                                        break;
                                }
                            }
                            positionValue[sd] += cRookValue[sqSrcMirror];
                            break;
                        case CANNON:
                            for (int j = 0; j < 4; j++)
                            {
                                if (ccPinDelta[pin, j])
                                    continue;
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
                            if (SAME_FILE(sqSrc, sqOppKing) || SAME_RANK(sqSrc, sqOppKing))
                                positionValue[sd] += 5;
                            break;
                        case KNIGHT:
                            if (pin > 0)
                                continue;
                            for (int j = 0; j < 4; j++)
                            {
                                if (pcSquares[sqSrc + ccKingDelta[j]] == 0)
                                {
                                    attackMap[sd, sqSrc + ccKnightDelta[j, 0]] = pc;
                                    attackMap[sd, sqSrc + ccKnightDelta[j, 1]] = pc;
                                }
                            }
                            positionValue[sd] += cKnightValue[sqSrcMirror];
                            break;
                        case PAWN:
                            if ((pin & 1) == 0)
                                attackMap[sd, SQUARE_FORWARD(sqSrc, sd)] = pc;
                            if ((pin & 2) == 0)
                                if (HOME_HALF[1 - sd, sqSrc])
                                {
                                    attackMap[sd, sqSrc + 1] = pc;
                                    attackMap[sd, sqSrc - 1] = pc;
                                }
                            positionValue[sd] += cKingPawnValue[sqSrcMirror];
                            break;
                        case BISHOP:
                            if (pin > 0)
                                continue;
                            for (int j = 0; j < 4; j++)
                            {
                                sqDst = sqSrc + ccGuardDelta[j];
                                if (HOME_HALF[sd, sqDst] && pcSquares[sqDst] == 0)
                                    attackMap[sd, sqDst + ccGuardDelta[j]] = pc;
                            }
                            positionValue[sd] += cBishopGuardValue[sqSrcMirror];
                            break;
                        case GUARD:
                            if (pin > 0)
                                continue;
                            for (int j = 0; j < 4; j++)
                            {
                                sqDst = sqSrc + ccGuardDelta[j];
                                if (IN_FORT[sqDst])
                                    attackMap[sd, sqDst] = pc;
                            }
                            positionValue[sd] += cBishopGuardValue[sqSrcMirror];
                            break;
                        default:
                            Debug.Fail("Unknown piece type");
                            break;
                    }
                    //ivpc[nStep, pc] = positionValue[sd] - posv0;
                }
                //帅所在的格没有保护
                attackMap[sd, sqPieces[bas + KING_FROM]] = 0;
            }

            int[] pair = new int[2];
            for (int sd = 0; sd < 2; sd++)
            {
                //棋盘上的子越多，炮的威力越大，马的威力越小
                pair[sd] += (int)(2.4 * totalPieces * nP[sd, CANNON]);  //可调系数
                pair[sd] -= (int)(0.9 * totalPieces * nP[sd, KNIGHT]);  //可调系数
                //兵卒的价值随着对方攻击子力的减少而增加（即增加了过河的可能性）
                int enemyAttack = nP[1 - sd, ROOK] * 2 + nP[1 - sd, CANNON] + nP[1 - sd, KNIGHT];
                int[] additionalPawnValue = { 28, 21, 15, 10, 6, 3, 2, 1, 0 };
                pair[sd] += nP[sd, PAWN] * additionalPawnValue[enemyAttack];
                //兵种不全扣分
                if (nP[sd, ROOK] == 0)
                    pair[sd] -= 30; //有车胜无车
                if (nP[sd, CANNON] == 0)
                    pair[sd] -= 20;
                if (nP[sd, KNIGHT] == 0)
                    pair[sd] -= 20;
                //缺相怕炮
                pair[sd] += (nP[sd, BISHOP] - nP[1 - sd, CANNON]) * 15;
                //缺仕怕车
                pair[sd] += (nP[sd, GUARD] - nP[1 - sd, ROOK]) * 15;
            }

            int[] connectivity = new int[2];
            captureMoves = new List<KeyValuePair<MOVE, int>>();
            for (int y = RANK_TOP; y <= RANK_BOTTOM; y++)
                for (int x = FILE_LEFT; x <= FILE_RIGHT; x++)
                {
                    int conn00 = connectivity[0], conn01 = connectivity[1];
                    sqDst = XY2Coord(x, y);
                    pcDst = pcSquares[sqDst];
                    int sd = SIDE(pcDst);
                    if (sd != -1)
                    {
                        int attack = attackMap[1 - sd, sqDst];
                        int protect = attackMap[sd, sqDst];
                        if (attack > 0)
                        {
                            int[] cnAttackScore = { 0, 20, 12, 8, 8, 4, 6, 6 };
                            if (protect > 0)
                            {
                                if (sd == sdPlayer)
                                    if (cnPieceValue[pcDst] > cnPieceValue[attack])
                                        connectivity[1 - sd] += cnAttackScore[cnPieceKinds[pcDst]];
                                    else
                                        connectivity[1 - sd] += 5;
                                else
                                {
                                    connectivity[1 - sd] += Math.Max(cnPieceValue[pcDst] - cnPieceValue[attack], 5);
                                    MOVE mv = new MOVE(sqPieces[attack], sqDst, attack, pcDst);
                                    captureMoves.Add(new KeyValuePair<MOVE, int>(mv, cnPieceValue[pcDst] - cnPieceValue[attack]));
                                }
                            }
                            else
                            {
                                if (sd == sdPlayer)
                                    connectivity[1 - sd] += cnAttackScore[cnPieceKinds[pcDst]];
                                else  //如果轮到对方走棋，可以直接吃无根子
                                {
                                    connectivity[1 - sd] += cnPieceValue[pcDst] * 3 / 4;
                                    MOVE mv = new MOVE(sqPieces[attack], sqDst, attack, pcDst);
                                    captureMoves.Add(new KeyValuePair<MOVE, int>(mv, cnPieceValue[pcDst]));
                                }
                            }
                        }
                        else if (protect > 0)
                            connectivity[sd] += 2;
                    }
                    else
                    {
                        for (sd = 0; sd < 2; sd++)
                            if (BannedGrids[sd, sqDst])
                                tacticValue[1 ^ sd] += attackMap[1 - sd, sqDst] > 0 ? cDiscoveredAttack[cnPieceKinds[attackMap[1 - sd, sqDst]]] : 2;
                            //机动性, 不考虑炮的空射，因为炮的射界与活动范围不同，且炮架可能是对方的车、炮或兵、帅
                            else if (attackMap[sd, sqDst] > 0 && cnPieceKinds[attackMap[sd, sqDst]] != CANNON && attackMap[1 - sd, sqDst] == 0)
                                connectivity[sd] += 2;
                    }
                    connectivityMap[0, sqDst] = connectivity[0] - conn00;
                    connectivityMap[1, sqDst] = connectivity[1] - conn01;
                }

            int scoreRed = materialValue[0] + positionValue[0] + pair[0] + connectivity[0] + tacticValue[0];
            int scoreBlack = materialValue[1] + positionValue[1] + pair[1] + connectivity[1] + tacticValue[1];

            int total = scoreRed - scoreBlack;
            //ivpc[nStep, 0] = nStep;
            //ivpc[nStep, 1] = total;
            //ivpc[nStep, 2] = scoreRed;
            //ivpc[nStep, 3] = scoreBlack;
            //ivpc[nStep, 4] = materialValue[0];
            //ivpc[nStep, 5] = materialValue[1];
            //ivpc[nStep, 6] = positionValue[0];
            //ivpc[nStep, 7] = positionValue[1];
            //ivpc[nStep, 8] = connectivity[0];
            //ivpc[nStep, 9] = connectivity[1];
            //ivpc[nStep, 10] = pair[0];
            //ivpc[nStep, 11] = pair[1];
            //ivpc[nStep, 12] = tacticValue[0];
            //ivpc[nStep, 13] = tacticValue[1];
            return sdPlayer == 0 ? total : -total;
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
                for (int pc = bas; pc < bas + 16; pc++)
                {
                    int sq = sqPieces[pc];
                    if (sq > 0)
                    {
                        int pcKind = cnPieceKinds[pc];
                        int sqMirror = sd == 0 ? sq : SQUARE_FLIP(sq);
                        totalPieces++;
                        nP[sd, pcKind]++;
                        materialValue[sd] += cnPieceValue[pc];
                        switch (pcKind)
                        {
                            case ROOK:
                                if (SAME_FILE(sq, sqOppKing) || SAME_RANK(sq, sqOppKing))
                                    positionValue[sd] += 10;
                                positionValue[sd] += cRookValue[sqMirror];
                                break;
                            case CANNON:
                                if (SAME_FILE(sq, sqOppKing))
                                    positionValue[sd] += 20;
                                else if (SAME_RANK(sq, sqOppKing))
                                    positionValue[sd] += 12;
                                break;
                            case KNIGHT:
                                positionValue[sd] += cKnightValue[sqMirror];
                                //检查绊马腿
                                for (int j = 0; j < 4; j++)
                                {
                                    int sqPin = sq + ccKingDelta[j];
                                    if (pcSquares[sqPin] != 0)
                                        positionValue[sd] -= 8;
                                }
                                break;
                            case PAWN:
                            case KING:
                                positionValue[sd] += cKingPawnValue[sqMirror];
                                break;
                            case BISHOP:
                            case GUARD:
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
                pair[sd] += (int)(2.4 * totalPieces * nP[sd, CANNON]);  //可调系数
                pair[sd] -= (int)(0.9 * totalPieces * nP[sd, KNIGHT]);  //可调系数
                //兵卒的价值随着对方攻击子力的减少而增加（即增加了过河的可能性）
                int enemyAttack = nP[1 - sd, ROOK] * 2 + nP[1 - sd, CANNON] + nP[1 - sd, KNIGHT];
                int[] additionalPawnValue = { 28, 21, 15, 10, 6, 3, 2, 1, 0 };
                pair[sd] += nP[sd, PAWN] * additionalPawnValue[enemyAttack];
                //兵种不全扣分
                if (nP[sd, ROOK] == 0)
                    pair[sd] -= 30; //有车胜无车
                if (nP[sd, CANNON] == 0)
                    pair[sd] -= 20;
                if (nP[sd, KNIGHT] == 0)
                    pair[sd] -= 20;
                //缺相怕炮
                pair[sd] += (nP[sd, BISHOP] - nP[1 - sd, CANNON]) * 15;
                //缺仕怕车
                pair[sd] += (nP[sd, GUARD] - nP[1 - sd, ROOK]) * 15;
            }
            int scoreRed = materialValue[0] + positionValue[0] + pair[0];
            int scoreBlack = materialValue[1] + positionValue[1] + pair[1];

            int total = scoreRed - scoreBlack;
            //ivpc[nStep, 0] = nStep;
            //ivpc[nStep, 1] = total;
            //ivpc[nStep, 2] = scoreRed;
            //ivpc[nStep, 3] = scoreBlack;
            //ivpc[nStep, 4] = materialValue[0];
            //ivpc[nStep, 5] = materialValue[1];
            //ivpc[nStep, 6] = positionValue[0];
            //ivpc[nStep, 7] = positionValue[1];
            //ivpc[nStep, 10] = pair[0];
            //ivpc[nStep, 11] = pair[1];

            return sdPlayer == 0 ? total : -total;
        }

        public void TestEval()
        {
            //int Compare(KeyValuePair<string, int> a, KeyValuePair<string, int> b)
            //{
            //    return a.Value.CompareTo(b.Value);
            //}
            string sourceDirectory = @"G:\象棋\全局\1-23届五羊杯";
            IEnumerable<string> pgnFiles = Directory.EnumerateFiles(sourceDirectory, "*.pgn", SearchOption.AllDirectories);
            int nFile = 0;
            int totalMoves = 0;
            int totalSteps = 0;
            List<double> redDelta = new List<double>();
            List<double> blackDelta = new List<double>();
            List<double> seq = new List<double>();
            foreach (string fileName in pgnFiles)
            {
                Console.WriteLine(fileName.Substring(sourceDirectory.Length + 1));
                PgnFileStruct pgn = ReadPgnFile(fileName);
                List<MOVE> iMoveList = pgn.MoveList;
                nFile++;
                int nSteps = iMoveList.Count;
                totalSteps += nSteps;
                bool[] captures = new bool[nSteps];
                FromFEN(pgn.StartFEN);
                ivpc = new int[nSteps, 48];
                Complex_Evaluate();
                List<KeyValuePair<string, int>> mv_vals = new List<KeyValuePair<string, int>>();
                for (int i = 1; i < nSteps; i++)
                {
                    MOVE step = iMoveList[i];
                    captures[i] = pcSquares[step.sqDst] > 0;
                    if (pcSquares[step.sqDst] == 0)
                    {
                        mv_vals.Clear();
                        //List<MOVE> moves = GenerateMoves();
                        int bookmovevalue = 0;
                        string bookmovekey = step.ToString();
                        foreach (MOVE move in GenerateMoves())
                        {
                            if (move.pcDst == 0)
                            {
                                MovePiece(move);
                                string key = move.ToString();
                                int val = Complex_Evaluate();
                                mv_vals.Add(new KeyValuePair<string, int>(key, val));
                                UndoMovePiece(move);
                                if (key == bookmovekey)
                                    bookmovevalue = val;
                            }
                        }
                        if (i % 2 == 0) //从小到大排序
                            mv_vals.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b)
                            { return a.Value.CompareTo(b.Value); });
                        else  //从大到小排序
                            mv_vals.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b)
                            { return b.Value.CompareTo(a.Value); });
                        int index = mv_vals.IndexOf(new KeyValuePair<string, int>(bookmovekey, bookmovevalue));
                        seq.Add(index);
                        //totalMoves += moves.Count;
                        Console.WriteLine("{0}. Book move: {1} {2}", i, bookmovekey, index);
                        foreach (var m in mv_vals)
                        {
                            //Console.WriteLine("{0}. {1} {2} / {3}", j++, m.Key, m.Value, moves.Count);

                        }
                    }

                    MakeMove(step);
                    Console.WriteLine("-------------------");
                }
                for (int i = 1; i < nSteps; i += 2)
                {
                    if (!captures[i])
                        redDelta.Add(ivpc[i, 1] - ivpc[i - 1, 1]);
                }

                for (int i = 2; i < nSteps; i += 2)
                {
                    if (!captures[i])
                        blackDelta.Add(ivpc[i, 1] - ivpc[i - 1, 1]);
                }
                break;
            }
            double redMean = Statistics.Mean(redDelta);
            double redVar = Statistics.Variance(redDelta);
            Console.WriteLine("Red mean:{0}, var:{1}", redMean, redVar);
            double blackMean = Statistics.Mean(blackDelta);
            double blackVar = Statistics.Variance(blackDelta);
            Console.WriteLine("Black mean:{0}, var:{1}", blackMean, blackVar);
            Console.WriteLine("Score: red{0}, black{1}, average{2}", redVar / redMean, blackVar / blackMean, (redVar + blackVar) / (redMean - blackMean));
            Console.WriteLine("Move average sequence: {0} of {1}", Statistics.Mean(seq), totalMoves / totalSteps);

        }
    }
}
