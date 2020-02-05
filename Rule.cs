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
            for (int i = nstep - 4; i > nstep - stepList[nstep].halfMoveClock; i -= 2)
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
            bool bOppPerpCheck = true;
            for (int i = nstep; i >= repStart; i -= 2)
            {
                if (stepList[i].checking == 0)
                {
                    bOppPerpCheck = false;
                    break;
                }
            }
            bool bSelfPerpCheck = true;
            for (int i = nstep - 1; i >= repStart; i -= 2)
            {
                if (stepList[i].checking == 0)
                {
                    bSelfPerpCheck = false;
                    break;
                }
            }

            if (bOppPerpCheck && bSelfPerpCheck)
                //双方循环长将（解将反将）作和
                return RepititionResult.DRAW;
            //无论任何情况，单方一子长将或多子交替长将者均需变着，不变作负
            //二将一还将，长将者作负
            else if (bOppPerpCheck)
                return RepititionResult.WIN;
            //己方走暂不判断
            else if (bSelfPerpCheck)
                return RepititionResult.NONE;

            //3. 倒退棋局到上一次重复局面
            for (int i = nstep; i >= repStart; i--)
                UndoMovePiece(stepList[i].move);
            //先假设所有的棋子都被长捉，然后从集合里逐个排除
            SortedSet<int>[] PerpChase = new SortedSet<int>[2];
            PerpChase[0] = new SortedSet<int>();
            PerpChase[1] = new SortedSet<int>();
            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                for (int pc = bas + ROOK_FROM; pc <= bas + GUARD_TO; pc++)
                    if (sqPieces[pc] > 0)
                        PerpChase[sd].Add(pc);
            }

            for (int i = repStart; i <= nstep; i++)
            {
                GenAttackMap();

                //一子轮捉两子或多子作和。两子分别轮捉两子或多子亦作和局
                //本方被捉只在偶数层有，奇数层没有；对方被捉只在奇数层有，偶数层没有
                PerpChase[sdPlayer].RemoveWhere(delegate (int pc) { return !Chased(pc); });
                //对方的棋子在此层应该不被捉
                PerpChase[1 - sdPlayer].RemoveWhere(delegate (int pc) { return Chased(pc); });

                MovePiece(stepList[i].move);
            }

            //如果双方均长捉违例，则判和
            if (PerpChase[sdPlayer].Count > 0 && PerpChase[1 ^ sdPlayer].Count > 0)
                return RepititionResult.DRAW;
            //单方面长捉判负
            else if (PerpChase[sdPlayer].Count > 0)
                return RepititionResult.WIN;
            else if (PerpChase[1 - sdPlayer].Count > 0)
                return RepititionResult.NONE;   //暂时不做判断
            //默认不变作和
            return RepititionResult.DRAW;
        }

        //这里的捉指亚洲规则的捉，调用此函数前必须先执行GenAttMap()
        bool Chased(int pc)
        {
            int[] rulePieceValue =
            { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            4, 3, 3, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            4, 3, 3, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            };
            int sq = sqPieces[pc];
            int sd = SIDE(pc);
            int sdOpp = 1 - sd;
            int pcOpp = attackMap[sdOpp, sq];
            if (pcOpp == 0)
                return false;  //没有捉子，排除
            else if (cnPieceKinds[pc] == PIECE_PAWN && HOME_HALF[sd, sq])
                return false;                     //可以长捉未过河的兵
            else
            {
                int sqOpp = sqPieces[pcOpp];
                if (cnPieceKinds[pcOpp] == PIECE_PAWN || cnPieceKinds[pcOpp] == PIECE_KING)
                    return false;  //兵和将允许长捉其它子
                if (attackMap[sdOpp, sqOpp] == pc)
                    return false;  	//两子互捉，算成兑子，作和
                if (rulePieceValue[pc] <= rulePieceValue[pcOpp] && attackMap[sd, sq] > 0)
                    return false;   //攻击有根子不算捉，除非被攻击子价值更大
            }
            return true;
        }

        //该函数等同于Evaluate的内部函数，只是去掉位置数组赋值，以提高速度，并减少耦合
        //如发现任何bug，须一同修改
        void GenAttackMap()
        {
            attackMap = new int[2, 256];    //保存攻击该格的价值最低的棋子

            int sqSrc, sqDst, delta, pcDst;
            for (int sd = 0; sd < 2; sd++)
            {
                int bas = SIDE_TAG(sd);
                for (int pc = bas; pc < bas + 16; pc++)
                {
                    sqSrc = sqPieces[pc];
                    if (sqSrc == 0)
                        continue;
                    int pcKind = cnPieceKinds[pc];

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

        //http://blog.sina.com.cn/s/blog_5fd97aa90100vq5p.html
        public RepititionResult RuleTest(string filename)
        {
            PgnFileStruct pgn = ReadPgnFile(filename);
            FromFEN(pgn.StartFEN);
            foreach (MOVE mv in pgn.MoveList)
            {
                Console.WriteLine(mv);
                MakeMove(mv);
                RepititionResult rep = Repitition();
                switch (rep)
                {
                    case RepititionResult.WIN:
                        if (sdPlayer == 0)
                            Console.WriteLine("黑方负");
                        else
                            Console.WriteLine("红方负");
                        break;
                    case RepititionResult.LOSE:
                        if (sdPlayer == 0)
                            Console.WriteLine("黑方胜");
                        else
                            Console.WriteLine("红方胜");
                        break;
                    case RepititionResult.DRAW:
                        Console.WriteLine("不变作和");
                        break;
                    default:
                        break;
                }
            }
            return RepititionResult.NONE;
        }
    }
}
