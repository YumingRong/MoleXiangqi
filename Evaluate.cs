using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MathNet.Numerics.Statistics;

/*
    我编了2个版本的审局函数，打算杀局时用简单的，静态局面时用复杂的。
是否需要考虑每匹马的机动性不小于2？
 */

namespace MoleXiangqi
{
    partial class POSITION
    {
        //各种子力的价值
        const int MAT_KING = 0;
        const int MAT_ROOK = 130;
        const int MAT_CANNON = 60;
        const int MAT_KNIGHT = 60;
        const int MAT_PAWN = 10;
        const int MAT_BISHOP = 25;
        const int MAT_ADVISOR = 20;

        static readonly int[] cnPieceValue = { 0, MAT_KING, MAT_ROOK, MAT_CANNON, MAT_KNIGHT, MAT_PAWN, MAT_BISHOP, MAT_ADVISOR };

        int[] cKingPawnValue, cRookValue;
        int[] cKnightValue;
        int[] cBishopGuardValue;

        void InitEval()
        {
            //只列出左半边位置分数数组，以方便修改
            int[] cRookHalfValue = {
            10,  13,  13,  28,  28,
            8,   18,  18,  25,  30,
            8,   20,  18,  20,  22,
            13,  18,  23,  25,  22,
            17,  17,  19,  22,  22,
            15,  18,  15,  18,  15,
            8,   15,  15,  16,  14,
            0,   12,  10,  15,  10,
            8,   12,   8,  13,   2,
            0,   10,   8,  14, -10,
        };
            
            int[] cKingPawnHalfValue = {
            1,   3,   5,   7,   9,
            15,  20,  28,  34,  40,
            15,  20,  25,  29,  34,
            14,  18,  23,  25,  29,
            10,  14,  19,  22,  25,
            3,   0,  15,  0,   15,
            0,   0,   5,   0,   10,
            0,   0,   0,   0,   0,
            0,   0,   0,   5,   2,
            0,   0,   0,   11,  15,
        };

            int[] cKnightHalfValue = {
            10,   7,  5,   10,   6,
            12,  16,  24,  24,  18,
            16,  17,  24,  26,  27,
            13,  18,  21,  24,  23,
            11, 22,  18,  24,  24,
            8,  15,  24,  17,  24,
            5,   9,  12,  15,  17,
            2,   6,  9,  12,  10,
            0,   3,  6,  8,  -15,
            -5,  0,  3,   0, -10};

            int[] cBishopGuardHalfValue = {
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,   0,   0,   0,
             0,   0,  -3,   0,   0,
             0,   0,   0,   0,   0,
             -5,   0,  0,  -3,   5,
             0,   0,   0,   0,   5,
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
            cRookValue = InitEvalArray(cRookHalfValue);
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
                                positionValue[sd] += cRookValue[sqMirror];
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
        public int[,] attackMap, connectivityMap;
        public int Complex_Evaluate()
        {
            //举例：当头炮与对方的帅之间隔了自己的马和对方的相，
            //自己的马就放在DiscoveredAttack里，对方的相就在PinnedPieces里
            int[] tacticValue = new int[2];
            bool[] PinnedPieces = new bool[48];
            bool[,] BannedGrids = new bool[2, 256];
            int sqSrc, sqDst, pcDst, delta;

            int[] cDiscoveredAttack = { 0, 1, 25, 20, 20, 7, 3, 3 };
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
                    tacticValue[side] += cDiscoveredAttack[pcKind];
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
            attackMap = new int[2, 256];    //保存攻击该格的价值最低的棋子
            connectivityMap = new int[2, 256]; 

            //Generate attack map, from most valuable piece to cheap piece
            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                sqOppKing = sqPieces[OPP_SIDE_TAG(sd) + KING_FROM];
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
                        case PIECE_KING:
                            for (int i = 0; i < 4; i++)
                            {
                                sqDst = sqSrc + ccKingDelta[i];
                                if (IN_FORT[sqDst])
                                    attackMap[sd, sqDst] = pcKind;
                            }
                            positionValue[sd] += cKingPawnValue[sqSrcMirror];
                            break;
                        case PIECE_ROOK:
                            for (int j = 0; j < 4; j++)
                            {
                                delta = ccKingDelta[j];
                                for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                                {
                                    pcDst = pcSquares[sqDst];
                                    attackMap[sd, sqDst] = pcKind;
                                    if (pcDst != 0)
                                        break;
                                }
                            }
                            positionValue[sd] += cRookValue[sqSrcMirror];
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
                                            attackMap[sd, sqDst] = pcKind;
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
                        case PIECE_KNIGHT:
                            for (int j = 0; j < 4; j++)
                            {
                                if (pcSquares[sqSrc + ccKingDelta[j]] == 0)
                                {
                                    attackMap[sd, sqSrc + ccKnightDelta[j, 0]] = pcKind;
                                    attackMap[sd, sqSrc + ccKnightDelta[j, 1]] = pcKind;
                                }
                            }
                            positionValue[sd] += cKnightValue[sqSrcMirror];
                            break;
                        case PIECE_PAWN:
                            attackMap[sd, SQUARE_FORWARD(sqSrc, sd)] = pcKind;
                            if (HOME_HALF[1 - sd, sqSrc])
                            {
                                attackMap[sd, sqSrc + 1] = pcKind;
                                attackMap[sd, sqSrc - 1] = pcKind;
                            }
                            positionValue[sd] += cKingPawnValue[sqSrcMirror];
                            break;
                        case PIECE_BISHOP:
                            for (int j = 0; j < 4; j++)
                            {
                                sqDst = sqSrc + ccGuardDelta[j];
                                if (HOME_HALF[sd, sqDst] && pcSquares[sqDst] == 0)
                                    attackMap[sd, sqDst + ccGuardDelta[j]] = pcKind;
                            }
                            positionValue[sd] += cBishopGuardValue[sqSrcMirror];
                            break;
                        case PIECE_GUARD:
                            for (int j = 0; j < 4; j++)
                            {
                                sqDst = sqSrc + ccGuardDelta[j];
                                if (IN_FORT[sqDst])
                                    attackMap[sd, sqDst] = pcKind;
                            }
                            positionValue[sd] += cBishopGuardValue[sqSrcMirror];
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
                    int conn00 = connectivity[0], conn01 = connectivity[1];
                    int sq = XY2Coord(x, y);
                    int pc = cnPieceKinds[pcSquares[sq]];
                    int sd = SIDE(pcSquares[sq]);
                    if (sd != -1)
                    {
                        int attack = attackMap[1 - sd, sq];//攻击兵种
                        int protect = attackMap[sd, sq]; //保护兵种
                        if (attack > 0)
                        {
                            int[] cnAttackScore = { 0, 20, 12, 8, 8, 4, 6, 6 };
                            if (protect > 0)
                            {
                                if (sd == sdPlayer)
                                    if (cnPieceValue[pc] > cnPieceValue[attack])
                                        connectivity[1 - sd] += cnAttackScore[pc];
                                    else
                                        connectivity[1 - sd] += 5;
                                else
                                    connectivity[1 - sd] += Math.Max(cnPieceValue[pc] - cnPieceValue[attack], 5);
                            }
                            else
                            {
                                if (sd == sdPlayer)
                                    connectivity[1 - sd] += cnAttackScore[pc];
                                else  //如果轮到对方走棋，可以直接吃无根子
                                    connectivity[1 - sd] += cnPieceValue[pc] * 3 / 4;
                            }
                        }
                        else if (protect > 0)
                            connectivity[sd] += 2;
                    }
                    else
                    {
                        for (sd = 0; sd < 2; sd++)
                            if (BannedGrids[sd, sq])
                                tacticValue[1 ^ sd] += attackMap[1 - sd, sq] > 0 ? cDiscoveredAttack[attackMap[1 - sd, sq]] : 2;
                            //机动性, 不考虑炮的空射，因为炮的射界与活动范围不同，且炮架可能是对方的车、炮或兵、帅
                            else if (attackMap[sd, sq] > 0 && attackMap[sd,sq] != PIECE_CANNON && attackMap[1-sd, sq] == 0)   
                                connectivity[sd] += 2;
                    }
                    connectivityMap[0, sq] = connectivity[0] - conn00;
                    connectivityMap[1, sq] = connectivity[1] - conn01;
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

        public void TestEval()
        {
            //int Compare(KeyValuePair<string, int> a, KeyValuePair<string, int> b)
            //{
            //    return a.Value.CompareTo(b.Value);
            //}
            string sourceDirectory = @"J:\象棋\全局\1-23届五羊杯";
            IEnumerable<string> pgnFiles = Directory.EnumerateFiles(sourceDirectory, "*.PGN", SearchOption.AllDirectories);
            int nFile = 0;
            int totalMoves = 0;
            List<double> redDelta = new List<double>();
            List<double> blackDelta = new List<double>();
            List<double> seq = new List<double>();
            foreach (string fileName in pgnFiles)
            {
                Console.WriteLine(fileName.Substring(sourceDirectory.Length + 1));
                if (!ReadPgnFile(fileName))
                {
                    Console.WriteLine("Fail to read!" + fileName);
                    continue;
                }
                nFile++;
                int totalSteps = iMoveList.Count;
                bool[] captures = new bool[totalSteps];
                FromFEN(PGN.StartFEN);
                ivpc = new int[totalSteps, 48];
                Complex_Evaluate();
                List<KeyValuePair<string, int>> mv_vals = new List<KeyValuePair<string, int>>();
                for (int i = 1; i < totalSteps; i++)
                {
                    iMOVE step = iMoveList[i];
                    captures[i] = pcSquares[step.to] > 0;
                    if (pcSquares[step.to] == 0)
                    {
                        mv_vals.Clear();
                        List<MOVE> moves = GenerateMoves();
                        int bookmovevalue = 0;
                        string bookmovekey = iMove2Coord(step.from, step.to);
                        foreach (MOVE move in moves)
                        {
                            if (move.pcDst == 0)
                            {
                                MovePiece(move);
                                ivpc = new int[totalSteps, 48];
                                string key = iMove2Coord(move.sqSrc, move.sqDst);
                                int val = Complex_Evaluate();
                                mv_vals.Add(new KeyValuePair<string, int>(key, val));
                                UndoMovePiece(move);
                                if (key == bookmovekey)
                                    bookmovevalue = val;
                            }
                        }
                        if (i % 2 == 0)
                            mv_vals.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b)
                            { return a.Value.CompareTo(b.Value); });
                        else
                            mv_vals.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b)
                            { return b.Value.CompareTo(a.Value); });
                        int index = mv_vals.IndexOf(new KeyValuePair<string, int>(bookmovekey, bookmovevalue));
                        seq.Add(index);
                        totalMoves += moves.Count;
                        Console.WriteLine("{0}. Book move: {1} {2}", i, bookmovekey, index);
                        int j = 0;
                        foreach (var m in mv_vals)
                        {
                            Console.WriteLine("{0}. {1} {2} / {3}", j++, m.Key, m.Value, moves.Count);

                        }
                    }

                    MakeMove(step.from, step.to);
                    Console.WriteLine("-------------------");
                }
                for (int i = 1; i < totalSteps; i += 2)
                {
                    if (!captures[i])
                        redDelta.Add(ivpc[i, 1] - ivpc[i - 1, 1]);
                }

                for (int i = 2; i < totalSteps; i += 2)
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
            Console.WriteLine("Move average sequence: {0} of {1}", Statistics.Mean(seq), totalMoves / nFile);

        }
    }
}
