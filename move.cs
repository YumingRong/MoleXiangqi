using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MoleXiangqi
{
    public struct iMOVE
    {
        public int from;
        public int to;
        public string comment;

        public iMOVE(MOVE mv)
        {
            from = mv.sqSrc;
            to = mv.sqDst;
            comment = "";
        }

        public override string ToString()
        {
            return POSITION.iMove2Coord(from, to);
        }
    }

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

        public override string ToString()
        {
            return POSITION.iMove2Coord(sqSrc, sqDst);
        }
    }

    public struct STEP
    {
        public long zobrist;
        public int checking;
        public bool capture;
        public MOVE move;
    }

    public partial class POSITION
    {
        //Interface to graphic board. x, y is 0~9
        public static Tuple<int, int> GetMove(int x0, int y0, int x1, int y1)
        {
            return new Tuple<int, int>(XY2Coord(x0 + FILE_LEFT, y0 + RANK_TOP), XY2Coord(x1 + FILE_LEFT, y1 + RANK_TOP));
        }

        // 走法是否符合帅(将)的步长
        bool KING_SPAN(int sqSrc, int sqDst)
        {
            return ccLegalSpan[sqDst - sqSrc + 256] == 1;
        }

        // 走法是否符合仕(士)的步长
        bool ADVISOR_SPAN(int sqSrc, int sqDst)
        {
            return ccLegalSpan[sqDst - sqSrc + 256] == 2;
        }

        // 走法是否符合相(象)的步长
        bool BISHOP_SPAN(int sqSrc, int sqDst)
        {
            return ccLegalSpan[sqDst - sqSrc + 256] == 3;
        }

        // 相(象)眼的位置
        static int BISHOP_PIN(int sqSrc, int sqDst)
        {
            return (sqSrc + sqDst) >> 1;
        }

        // 马腿的位置
        static int KNIGHT_PIN(int sqSrc, int sqDst)
        {
            return sqSrc + ccKnightPin[sqDst - sqSrc + 256];
        }

        // 是否在河的同一边
        static bool SAME_HALF(int sqSrc, int sqDst)
        {
            return ((sqSrc ^ sqDst) & 0x80) == 0;
        }

        static int SQUARE_FORWARD(int sq, int sd)
        {
            return sq - 16 + (sd << 5);
        }

        // 是否在同一行
        static bool SAME_RANK(int sqSrc, int sqDst)
        {
            return ((sqSrc ^ sqDst) & 0xf0) == 0;
        }

        // 是否在同一列
        static bool SAME_FILE(int sqSrc, int sqDst)
        {
            return ((sqSrc ^ sqDst) & 0x0f) == 0;
        }

        public void MovePiece(MOVE mv)
        {
            Debug.Assert(IN_BOARD[mv.sqDst]);
            sqPieces[mv.pcDst] = 0;
            sqPieces[mv.pcSrc] = mv.sqDst;
            pcSquares[mv.sqSrc] = 0;
            pcSquares[mv.sqDst] = mv.pcSrc;
            sdPlayer ^= 1;
        }

        public void UndoMovePiece(MOVE mv)
        {
            sqPieces[mv.pcSrc] = mv.sqSrc;
            pcSquares[mv.sqSrc] = mv.pcSrc;
            sqPieces[mv.pcDst] = mv.sqDst;
            pcSquares[mv.sqDst] = mv.pcDst;
            sdPlayer ^= 1;
        }

        public void MakeMove(MOVE mv)
        {
            MovePiece(mv);
            moveStack.Push(mv);
            STEP step;
            step.move = mv;
            step.zobrist = stepList[stepList.Count - 1].zobrist ^ Zobrist.Get(mv.pcSrc, mv.sqSrc) ^ Zobrist.Get(mv.pcSrc, mv.sqDst);
            step.capture = false;
            step.checking = CheckedBy(sdPlayer); //暂时不必判断将军与否
            if (mv.pcDst > 0)
            {
                step.zobrist ^= Zobrist.Get(mv.pcDst, mv.sqDst);
                step.capture = true;
                halfMoveClock = 0;
            }
            stepList.Add(step);
        }

        public void UnmakeMove()
        {
            MOVE mv = moveStack.Pop();
            UndoMovePiece(mv);
            stepList.RemoveAt(stepList.Count - 1);
        }

        public void MakeMove(int from, int to)
        {
            MOVE move;
            move.sqSrc = from;
            move.sqDst = to;
            move.pcSrc = pcSquares[from];
            move.pcDst = pcSquares[to];
            MakeMove(move);
        }

    }
}
