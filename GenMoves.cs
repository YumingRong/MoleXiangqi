using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    public partial class POSITION
    {
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
                case PAWN:
                    if (sqDst == SQUARE_FORWARD(sqSrc, selfSide))
                        return true;
                    if (HOME_HALF[1 - selfSide, sqSrc] && (sqDst == sqSrc - 1 || sqDst == sqSrc + 1))
                        return true;
                    return false;
                default:
                    Debug.Fail("Unknown piece type");
                    return false;
            }
        }

        // 判断是否被将军
        public int CheckedBy(int side)
        {
            Debug.Assert(side == 0 || side == 1);
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
            List<MOVE> mvs;

            mvs = GenerateMoves();
            return mvs.Count == 0;
        }

        //在形如红马-黑车-黑将的棋型中，黑车是可以吃红马的. Record from and to square
        List<Tuple<int, int>> pinexception = new List<Tuple<int, int>>();
        bool[,] bannedGrids = new bool[2, 256];
        int[] DiscoverAttack = new int[48];   //store discover attack direction for each piece
        int[] PinnedPieces = new int[48];

        //0没有牵制，1纵向牵制，2横向牵制，3纵横牵制
        void FindAbsolutePin()
        {
            //举例：当头炮与对方的帅之间隔了自己的马和对方的相，
            //自己的马就放在DiscoveredAttack里，对方的相就在PinnedPieces里
            Array.Clear(bannedGrids, 0, 256 * 2);
            Array.Clear(DiscoverAttack, 0, 48);
            Array.Clear(PinnedPieces, 0, 48);
            pinexception.Clear();

            for (int side = 0; side < 2; side++)
            {
                int sqSrc, pcDst, delta, pindir;

                int bas, sqKing;
                //find absolute pin. 0没有牵制，1纵向牵制，2横向牵制，3纵横牵制
                bas = OPP_SIDE_TAG(side);
                sqKing = sqPieces[SIDE_TAG(side) + KING_FROM];

                //对阻挡将军的子进行判断
                void CheckBlocker(int pcBlocker, int direction)
                {
                    if (SIDE(pcBlocker) == side)
                        PinnedPieces[pcBlocker] |= direction;
                    else
                        DiscoverAttack[pcBlocker] |= direction;
                }

                for (int pc = bas + ROOK_FROM; pc <= bas + ROOK_TO; pc++)
                {
                    delta = pindir = 0;
                    sqSrc = sqPieces[pc];
                    if (SAME_FILE(sqSrc, sqKing))
                    {
                        delta = Math.Sign(sqKing - sqSrc) * 16;
                        pindir = 1;
                    }
                    else if (SAME_RANK(sqSrc, sqKing))
                    {
                        delta = Math.Sign(sqKing - sqSrc);
                        pindir = 2;
                    }
                    if (delta != 0)
                    {
                        int pcBlocker = 0, nblock = 0;
                        for (int sq = sqSrc + delta; sq != sqKing; sq += delta)
                        {
                            if (pcSquares[sq] != 0)
                            {
                                pcBlocker = pcSquares[sq];
                                nblock++;
                            }
                        }
                        Debug.Assert(pindir != 0);
                        if (nblock == 1)
                            CheckBlocker(pcBlocker, pindir);
                    }
                }

                for (int pc = bas + CANNON_FROM; pc <= bas + CANNON_TO; pc++)
                {
                    delta = pindir = 0;
                    sqSrc = sqPieces[pc];
                    if (SAME_FILE(sqSrc, sqKing))
                    {
                        delta = Math.Sign(sqKing - sqSrc) * 16;
                        pindir = 1;
                    }
                    else if (SAME_RANK(sqSrc, sqKing))
                    {
                        delta = Math.Sign(sqKing - sqSrc);
                        pindir = 2;
                    }
                    if (delta != 0)
                    {
                        int nblock = 0;
                        for (int sq = sqSrc + delta; sq != sqKing; sq += delta)
                        {
                            if (pcSquares[sq] != 0)
                                nblock++;
                        }
                        if (nblock == 2)
                        {
                            for (int sq = sqSrc + delta; sq != sqKing; sq += delta)
                                if (pcSquares[sq] > 0)
                                {
                                    Debug.Assert(pindir != 0);
                                    CheckBlocker(pcSquares[sq], pindir);
                                }
                        }
                        else if (nblock == 0)
                        {
                            for (int sq = sqSrc + delta; sq != sqKing; sq += delta)
                                bannedGrids[side, sq] = true;
                        }
                    }
                }

                // 3. 判断将是否被马威胁(以仕/士的步长当作马腿)
                for (int i = 0; i < 4; i++)
                {
                    int sqBlocker = sqKing + ccGuardDelta[i];
                    int pcBlocker = pcSquares[sqBlocker];
                    if (pcBlocker != 0)
                        for (int j = 0; j < 2; j++)
                        {
                            int sqKnight = sqKing + ccKnightCheckDelta[i, j];
                            pcDst = pcSquares[sqKnight];
                            if (cnPieceTypes[pcDst] == bas + KNIGHT)
                            {
                                CheckBlocker(pcBlocker, 3);
                                //在形如红马-黑车-黑将的棋型中，黑车是可以吃红马的
                                if (IsLegalMove(sqBlocker, sqKnight))
                                    pinexception.Add(new Tuple<int, int>(sqBlocker, sqKnight));
                            }
                        }
                }

                //4. Is there king face to face risk?
                sqSrc = sqPieces[32 + KING_FROM];
                int sqDst = sqPieces[16 + KING_FROM];
                if (SAME_FILE(sqSrc, sqDst))
                {
                    int pcBlocker = 0, nblock = 0;
                    for (int i = sqSrc + 16; i < sqDst; i += 16)
                        if (pcSquares[i] > 0)
                        {
                            pcBlocker = pcSquares[i];
                            nblock++;
                        }
                    if (nblock == 1)
                        CheckBlocker(pcBlocker, 1);
                }
            }
        }

        //The move generated are after check test. So they are already legal. 
        //if FindAbsolutePin has been done right before GenerateMoves, set pinDone true
        List<MOVE> GenerateMoves(bool pinDone = false)
        {
            int sqSrc, sqDst, pcSrc, pcDst;
            int myBase, oppBase;
            int delta;
            List<MOVE> mvs = new List<MOVE>();

            myBase = SIDE_TAG(sdPlayer);
            oppBase = OPP_SIDE_TAG(sdPlayer);
            if (!pinDone)
                FindAbsolutePin();
            void AddMove()
            {
                if (bannedGrids[sdPlayer, sqDst])
                    return;
                MOVE mv = new MOVE(sqSrc, sqDst, pcSrc, pcDst);
                int mySide = sdPlayer;
                if (PinnedPieces[pcSrc] == 0)
                {
                    //如果被照将，先试试走棋后，照将着法是否仍然成立
                    if (stepList[stepList.Count - 1].move.checking)
                    {
                        MovePiece(mv);
                        int sqKing = sqPieces[SIDE_TAG(mySide) + KING_FROM];
                        if (!IsLegalMove(stepList[stepList.Count - 1].move.sqDst, sqKing))
                        {
                            if (CheckedBy(mySide) == 0)
                                mvs.Add(mv);
                        }
                        UndoMovePiece(mv);
                    }
                    else
                        mvs.Add(mv);
                }
                else
                {
                    MovePiece(mv);
                    if (CheckedBy(mySide) == 0)
                        mvs.Add(mv);
                    UndoMovePiece(mv);
                }
            }

            for (int i = ROOK_FROM; i <= ROOK_TO; i++)
            {
                pcSrc = myBase + i;
                sqSrc = sqPieces[pcSrc];
                if (sqSrc == 0)
                    continue;

                for (int j = 0; j < 4; j++)
                {
                    if (ccPinDelta[PinnedPieces[pcSrc], j])
                        continue;
                    delta = ccKingDelta[j];
                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                    {
                        pcDst = pcSquares[sqDst];
                        if (pcDst == 0)
                            AddMove();
                        else
                        {
                            if ((pcDst & oppBase) != 0)
                                AddMove();
                            break;
                        }
                    }
                }
            }
            for (int i = CANNON_FROM; i <= CANNON_TO; i++)
            {
                pcSrc = myBase + i;
                sqSrc = sqPieces[pcSrc];
                if (sqSrc == 0)
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    if (ccPinDelta[PinnedPieces[pcSrc], j])
                        continue;
                    delta = ccKingDelta[j];
                    for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                    {
                        pcDst = pcSquares[sqDst];
                        if (pcDst == 0)
                            AddMove();
                        else
                        {
                            for (sqDst += delta; IN_BOARD[sqDst]; sqDst += delta)
                            {
                                pcDst = pcSquares[sqDst];
                                if (pcDst != 0)
                                {
                                    if ((pcDst & oppBase) != 0)
                                        AddMove();
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
                pcSrc = myBase + i;
                sqSrc = sqPieces[pcSrc];
                if (sqSrc == 0 || PinnedPieces[pcSrc] > 0)
                    continue;
                for (int j = 0; (sqDst = csqKnightMoves[sqSrc, j]) > 0; j++)
                {
                    if (pcSquares[csqKnightPins[sqSrc, j]] == 0)
                    {
                        pcDst = pcSquares[sqDst];
                        if ((pcDst & myBase) == 0)
                            AddMove();
                    }
                }
            }

            for (int i = PAWN_FROM; i <= PAWN_TO; i++)
            {
                pcSrc = myBase + i;
                sqSrc = sqPieces[pcSrc];
                if (sqSrc == 0)
                    continue;
                sqDst = SQUARE_FORWARD(sqSrc, sdPlayer);
                if (IN_BOARD[sqDst])
                {
                    pcDst = pcSquares[sqDst];
                    if ((pcDst & myBase) == 0)
                        AddMove();
                }
                if ((PinnedPieces[pcSrc] & 2) == 0)
                {
                    sqDst = sqSrc - 1;
                    if (HOME_HALF[1 - sdPlayer, sqDst])
                    {
                        pcDst = pcSquares[sqDst];
                        if ((pcDst & myBase) == 0)
                            AddMove();
                    }
                    sqDst = sqSrc + 1;
                    if (HOME_HALF[1 - sdPlayer, sqDst])
                    {
                        pcDst = pcSquares[sqDst];
                        if ((pcDst & myBase) == 0)
                            AddMove();
                    }
                }
            }

            for (int i = BISHOP_FROM; i <= BISHOP_TO; i++)
            {
                pcSrc = myBase + i;
                sqSrc = sqPieces[pcSrc];
                if (sqSrc == 0 || PinnedPieces[pcSrc] > 0)
                    continue;
                for (int j = 0; (sqDst = csqBishopMoves[sqSrc, j]) > 0; j++)
                {
                    if (pcSquares[(sqDst + sqSrc) / 2] == 0)
                    {
                        pcDst = pcSquares[sqDst];
                        if ((pcDst & myBase) == 0)
                            AddMove();
                    }
                }
            }

            for (int i = GUARD_FROM; i <= GUARD_TO; i++)
            {
                pcSrc = myBase + i;
                sqSrc = sqPieces[pcSrc];
                if (sqSrc == 0 || PinnedPieces[pcSrc] > 0)
                    continue;
                for (int j = 0; (sqDst = csqAdvisorMoves[sqSrc, j]) > 0; j++)
                {
                    pcDst = pcSquares[sqDst];
                    if ((pcDst & myBase) == 0)
                        AddMove();
                }
            }

            pcSrc = myBase + KING_FROM;
            sqSrc = sqPieces[pcSrc];
            for (int i = 0; (sqDst = csqKingMoves[sqSrc, i]) != 0; i++)
            {
                pcDst = pcSquares[sqDst];
                if ((pcDst & myBase) == 0)
                {
                    MOVE mv = new MOVE(sqSrc, sqDst, myBase + KING_FROM, pcDst);
                    MovePiece(mv);
                    if (CheckedBy(1 - sdPlayer) == 0)
                        mvs.Add(mv);
                    UndoMovePiece(mv);
                }
            }

            foreach (Tuple<int, int> pin in pinexception)
            {
                MOVE mv = new MOVE(pin.Item1, pin.Item2, pcSquares[pin.Item1], pcSquares[pin.Item2]);
                mvs.Add(mv);
            }
            return mvs;
        }

        //If FindAbsolutePin has been done right before GenerateMoves, set pinDone true
        List<int>[,] AttackMap = new List<int>[2, 256];
        void GenAttackMap(bool pinDone = false)
        {
            if (!pinDone)
                FindAbsolutePin();

            int sqSrc, sqDst, pcDst, delta;
            for (int side = 0; side < 2; side++)
            {
                foreach (int sq in cboard90)
                    AttackMap[side, sq].Clear();
                //find absolute pin. 0没有牵制，1纵向牵制，2横向牵制，3纵横牵制
                //Generate enemy attack map, from most valuable piece to cheap piece

                int bas = SIDE_TAG(side);
                for (int pc = bas + 15; pc >= bas; pc--)
                {
                    sqSrc = sqPieces[pc];
                    if (sqSrc == 0)
                        continue;
                    int pin = PinnedPieces[pc];

                    switch (cnPieceKinds[pc])
                    {
                        case KING:
                            for (int i = 0; (sqDst = csqKingMoves[sqSrc, i]) != 0; i++)
                                AttackMap[side, sqDst].Add(pc);
                            break;
                        case ROOK:
                            for (int j = 0; j < 4; j++)
                            {
                                if (ccPinDelta[pin, j])
                                    continue;
                                delta = ccKingDelta[j];
                                for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                                {
                                    AttackMap[side, sqDst].Add(pc);
                                    pcDst = pcSquares[sqDst];
                                    if (pcDst != 0)
                                        break;
                                }
                            }
                            break;
                        case CANNON:
                            for (int j = 0; j < 4; j++)
                            {
                                if (ccPinDelta[pin, j])
                                    continue;
                                delta = ccKingDelta[j];
                                for (sqDst = sqSrc + delta; IN_BOARD[sqDst]; sqDst += delta)
                                {
                                    if (pcSquares[sqDst] != 0) //炮架
                                    {
                                        for (sqDst += delta; IN_BOARD[sqDst]; sqDst += delta)
                                        {
                                            AttackMap[side, sqDst].Add(pc);
                                            if (pcSquares[sqDst] != 0) //直瞄点
                                                goto NextFor;
                                        }
                                    }
                                }
                            NextFor:;
                            }
                            break;
                        case KNIGHT:
                            if (pin > 0)
                                continue;
                            for (int j = 0; (sqDst = csqKnightMoves[sqSrc, j]) > 0; j++)
                            {
                                if (pcSquares[csqKnightPins[sqSrc, j]] == 0)
                                    AttackMap[side, sqDst].Add(pc);
                            }
                            break;
                        case PAWN:
                            sqDst = SQUARE_FORWARD(sqSrc, side);
                            if (IN_BOARD[sqDst])
                                AttackMap[side, sqDst].Add(pc);
                            if ((pin & 2) == 0)
                            {
                                if (HOME_HALF[1 - side, sqSrc - 1])
                                    AttackMap[side, sqSrc - 1].Add(pc);
                                if (HOME_HALF[1 - side, sqSrc + 1])
                                    AttackMap[side, sqSrc + 1].Add(pc);
                            }
                            break;
                        case BISHOP:
                            if (pin > 0)
                                continue;
                            for (int j = 0; (sqDst = csqBishopMoves[sqSrc, j]) > 0; j++)
                            {
                                if (pcSquares[(sqDst + sqSrc) / 2] == 0)
                                    AttackMap[side, sqDst].Add(pc);
                            }
                            break;
                        case GUARD:
                            if (pin > 0)
                                continue;
                            for (int j = 0; (sqDst = csqAdvisorMoves[sqSrc, j]) > 0; j++)
                                AttackMap[side, sqDst].Add(pc);
                            break;
                    }
                }

            }
            foreach (Tuple<int, int> pin in pinexception)
            {
                int pc = pcSquares[pin.Item1];
                AttackMap[SIDE(pc), pin.Item2].Add(pc);
                AttackMap[SIDE(pc), pin.Item2].Sort(delegate (int x, int y) { return y.CompareTo(x); });
            }
        }

    }
}