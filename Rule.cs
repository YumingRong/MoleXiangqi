using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    public enum RepititionResult { WIN = G.RULEWIN, DRAW = 0, LOSE = -G.RULEWIN, NONE = -1 };

    partial class POSITION
    {
        // 重复局面检测。支持亚洲规则
        public RepititionResult Repitition()
        {
            int repStart = -1;
            int nstep = stepList.Count - 1;
            // 1. 首先检测历史局面中是否有当前局面，如果没有，就用不着判断了
            for (int i = nstep - 4; i > nstep - HalfMoveClock; i -= 2)
            {
                if (stepList[i].zobrist == Key)
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

            //这里的捉指亚洲规则的捉，调用此函数前必须先执行GenAttMap()
            bool Chased(int pc)
            {
                Debug.Assert(pc >= 16 && pc < 48);
                int[] rulePieceValue =
                {
                    0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                    4, 3, 3, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                    4, 3, 3, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                };
                int sq = sqPieces[pc];
                Debug.Assert(IN_BOARD[sq]);
                int sd = SIDE(pc);
                int sdOpp = 1 - sd;
                if (attackMap[sdOpp, sq].Count == 0)
                    return false;  //没有捉子，排除
                if (cnPieceKinds[pc] == PAWN && HOME_HALF[sd, sq])
                    return false;                     //可以长捉未过河的兵
                int pcOpp = attackMap[sdOpp, sq][0];
                if (cnPieceKinds[pcOpp] == PAWN || cnPieceKinds[pcOpp] == KING)
                    return false;  //兵和将允许长捉其它子
                int sqOpp = sqPieces[pcOpp];
                if (attackMap[sd, sqOpp].Contains(pc))
                    return false;   //两子互捉，算成兑子，作和
                if (rulePieceValue[pc] <= rulePieceValue[pcOpp] && attackMap[sd, sq].Count > 0)
                    return false;   //攻击有根子不算捉，除非被攻击子价值更大
                                    //如果吃子导致被将军，则该棋子被牵制中，不算捉子
                MOVE mv = new MOVE(sqOpp, sq, pcOpp, pc);
                MovePiece(mv);
                int suicide = CheckedBy(sdOpp);
                UndoMovePiece(mv);
                if (suicide > 0)
                    return false;
                return true;
            }
        }

        //http://blog.sina.com.cn/s/blog_5fd97aa90100vq5p.html
        public RepititionResult RuleTest(string filename)
        {
            Debug.Assert(filename != null);
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
