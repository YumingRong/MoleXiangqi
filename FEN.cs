using System;
using System.Text;
using System.Diagnostics;

namespace MoleXiangqi
{
    public partial class POSITION
    {
        public const string cszStartFen = "rnbgkgbnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBGKGBNR w - - 0 0";

        static int Fen2Piece(char nArg)
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

        //该函数相当于UCCI的position指令，文件格式正确返回true
        public bool FromFEN(string szFen)
        {
            if (String.IsNullOrEmpty(szFen))
                return false;
            // 棋盘上增加棋子
            void AddPiece(int sq, int pc)
            {
                while (sqPieces[pc] != 0)
                    pc++;
                sqPieces[pc] = sq;
                pcSquares[sq] = pc;
            }

            string[] subs = szFen.Split(' ');
            if (subs.Length != 6)
                return false;
            //棋盘有10行
            string[] rows = subs[0].Split('/');
            if (rows.Length != 10)
                return false;
            // FEN串的识别包括以下几个步骤：
            // 1. 初始化，清空棋盘
            ClearBoard();

            // 2. 读取棋盘上的棋子
            for (int  y = 0; y < 10; y++)
            {
                int x = 0;
                foreach (char c in rows[y])
                {
                    if (Char.IsNumber(c))
                    {
                        x += c - '0';
                    }
                    else if (Char.IsUpper(c))
                    {
                        AddPiece(UI_XY2Coord(x, y), Fen2Piece(c) + SIDE_TAG(0));
                        x++;
                    }
                    else if (Char.IsLower(c))
                    {
                        AddPiece(UI_XY2Coord(x, y), Fen2Piece(c) + SIDE_TAG(1));
                        x++;
                    }
                    else
                        return false;
                }
            }

            // 3. 确定轮到哪方走
            if (subs[1] == "b")
                sdPlayer = 1;
            else
                sdPlayer = 0;

            stepList.Clear();
            Key = CalculateZobrist();
            /*ElephantBoard向引擎传递局面时，<fen_string>总是最近一次吃过子的局面(或开始局面)，
               * 后面所有的着法都用moves选项来传递给引擎，这样就包含了判断自然限着和长打的历史信息，
               * 这些信息可由引擎来处理。
               */
            HalfMoveClock = 0;
            RECORD step;
            step.move = new MOVE(0,0,0,0);
            step.zobrist = Key;
            //step.checking = CheckedBy(sdPlayer);
            step.halfMoveClock = HalfMoveClock; 
            stepList.Add(step);
            return true;
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
            lpFen.Append(HalfMoveClock);
            lpFen.Append(" ");
            lpFen.Append(stepList.Count - 1);
            return lpFen.ToString();
        }

        public static string MOVE2ICCS(int from, int to)
        {      // 把着法转换成ICCS字符串
            char[] ret = new char[5];
            ret[0] = Convert.ToChar(FILE_X(from) - FILE_LEFT + 'A');
            ret[1] = Convert.ToChar('9' - RANK_Y(from) + RANK_TOP);
            ret[2] = '-';
            ret[3] = Convert.ToChar(FILE_X(to) - FILE_LEFT + 'A');
            ret[4] = Convert.ToChar('9' - RANK_Y(to) + RANK_TOP);
            return new string(ret);
        }

        public static Tuple<int, int> ICCS2Move(string dwMoveStr)
        { // 把字符串转换成着法
            Debug.Assert(dwMoveStr != null);
            dwMoveStr = dwMoveStr.ToUpper();
            int sqSrc, sqDst;
            sqSrc = UI_XY2Coord(dwMoveStr[0] - 'A', '9' - dwMoveStr[1]);
            sqDst = UI_XY2Coord(dwMoveStr[3] - 'A', '9' - dwMoveStr[4]);
            Debug.Assert(IN_BOARD[sqSrc]);
            Debug.Assert(IN_BOARD[sqDst]);
            return new Tuple<int, int>(sqSrc, sqDst);
        }

    }
}
