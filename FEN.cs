using System;
using System.Text;
using System.Diagnostics;

namespace MoleXiangqi
{
    public partial class POSITION
    {
        public const string cszStartFen = "rnbgkgbnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBGKGBNR w - - 0 0";

        int Fen2Piece(char nArg)
        {
            // FEN串中棋子标识
            if (Char.IsLower(nArg))
                nArg = Char.ToUpper(nArg);
            switch (nArg)
            {
                case 'P':
                    return PAWN_FROM;
                case 'R':
                    return ROOK_FROM;
                case 'C':
                    return CANNON_FROM;
                case 'N':
                case 'H':
                    return KNIGHT_FROM;
                case 'B':
                case 'E':
                    return BISHOP_FROM;
                case 'A':
                case 'G':
                    return GUARD_FROM;
                case 'K':
                    return KING_FROM;
                default:
                    return 0;
            }
        }

        public void FromFEN(string szFen)
        {

            // 棋盘上增加棋子
            void AddPiece(int sq, int pc)
            {
                while (sqPieces[pc] != 0)
                    pc++;
                sqPieces[pc] = sq;
                pcSquares[sq] = pc;
            }

            string[] subs = szFen.Split(' ');
            // FEN串的识别包括以下几个步骤：
            // 1. 初始化，清空棋盘
            ClearBoard();

            //这边跳去SetIrrev()
            // 2. 读取棋盘上的棋子
            int x, y;
            x = RANK_TOP;
            y = FILE_LEFT;

            foreach (char c in subs[0])
            {
                if (c != ' ')
                {
                    if (c == '/')
                    {
                        y = FILE_LEFT;
                        x++;
                        if (x > RANK_BOTTOM)
                            break;
                    }
                    else if (Char.IsNumber(c))
                    {
                        y += c - '0';
                    }
                    else if (Char.IsUpper(c))
                    {
                        AddPiece(XY2Coord(y, x), Fen2Piece(c) + SIDE_TAG(0));
                        y++;
                    }
                    else if (Char.IsLower(c))
                    {
                        AddPiece(XY2Coord(y, x), Fen2Piece(c) + SIDE_TAG(1));
                        y++;
                    }
                }
                else
                    break;
            }

            // 3. 确定轮到哪方走
            if (subs[1] == "b")
                sdPlayer = 1;
            else
                sdPlayer = 0;

            stepList.Clear();
            STEP step;
            step.move = new MOVE();
            step.zobrist = CalculateZobrist();
            step.capture = false;
            step.checking = 0;
            //step.halfMoveClock = Convert.ToInt32(subs[4]);
            step.halfMoveClock = 0; //为简单起见，否则要再设一个变量
            stepList.Add(step);
        }

        // 生成FEN串
        public string ToFen()
        {
            string Piece2Fen;   //参见cnPieceTypes

            Piece2Fen = new string(' ', 16);
            string cszPieceBytes = "KRRCCNNPPPPPBBGG";
            Piece2Fen += cszPieceBytes + cszPieceBytes.ToLower();
            Debug.Assert(Piece2Fen.Length == 48);

            int i, j, k, pc;
            StringBuilder lpFen = new StringBuilder();

            for (i = RANK_TOP; i <= RANK_BOTTOM; i++)
            {
                k = 0;
                for (j = FILE_LEFT; j <= FILE_RIGHT; j++)
                {
                    pc = pcSquares[XY2Coord(j, i)];
                    if (pc != 0)
                    {
                        if (k > 0)
                        {
                            lpFen.Append(k);
                            k = 0;
                        }
                        lpFen.Append(Piece2Fen[pc]);
                    }
                    else
                        k++;
                }
                if (k > 0)
                    lpFen.Append(k);
                lpFen.Append('/');
            }
            lpFen[lpFen.Length - 1] = ' '; // 把最后一个'/'替换成' '
            lpFen.Append(sdPlayer == 0 ? 'r' : 'b');
            lpFen.Append(" - - ");
            lpFen.Append(stepList[stepList.Count - 1].halfMoveClock);
            lpFen.Append(" ");
            lpFen.Append(stepList.Count - 1);
            return lpFen.ToString();
        }

        public static string iMove2Coord(int from, int to)
        {      // 把着法转换成字符串
            char[] ret = new char[5];
            ret[0] = Convert.ToChar(FILE_X(from) - FILE_LEFT + 'A');
            ret[1] = Convert.ToChar('9' - RANK_Y(from) + RANK_TOP);
            ret[2] = '-';
            ret[3] = Convert.ToChar(FILE_X(to) - FILE_LEFT + 'A');
            ret[4] = Convert.ToChar('9' - RANK_Y(to) + RANK_TOP);
            return new string(ret);
        }

        //public static Point Coord2Move(string dwMoveStr)
        //{ // 把字符串转换成着法
        //    int sqSrc, sqDst;
        //    sqSrc = XY2Coord(dwMoveStr[0] - 'a' + FILE_LEFT, '9' - dwMoveStr[1] + RANK_TOP);
        //    sqDst = XY2Coord(dwMoveStr[3] - 'a' + FILE_LEFT, '9' - dwMoveStr[4] + RANK_TOP);
        //    Debug.Assert(IN_BOARD[sqSrc]);
        //    Debug.Assert(IN_BOARD[sqDst]);
        //    return MOVE(sqSrc, sqDst);
        //}

    }
}
