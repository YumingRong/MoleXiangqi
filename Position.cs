using System;
using System.Collections.Generic;

namespace MoleXiangqi
{
    public partial class POSITION
    {
        public POSITION()
        {
            pcSquares = new int[256];
            sqPieces = new int[48];
            stepList = new List<RECORD>();
            InitPreGen();
            InitPGN();
            InitEval();
            InitSearch();
            pinexception = new List<Tuple<int, int>>();
        }

        // 基本成员
        public int sdPlayer;             // 轮到哪方走，0表示红方，1表示黑方
        public int[] pcSquares;         // 每个格子放的棋子，0表示没有棋子, size = 256
        public int[] sqPieces;          // 每个棋子放的位置，0表示被吃, size = 48
        public List<RECORD> stepList;

        // 每种子力的类型编号，按子力价值排序
        const int EMPTY = 0;
        const int KING = 1;
        const int ROOK = 2;
        const int CANNON = 3;
        const int KNIGHT = 4;
        const int PAWN = 5;
        const int BISHOP = 6;
        const int GUARD = 7;       //仕更准确的翻译是guard，而不是advisor


        // 每种子力的开始序号和结束序号
        const int KING_FROM = 0;
        const int KING_TO = 0;
        const int ROOK_FROM = 1;
        const int ROOK_TO = 2;
        const int CANNON_FROM = 3;
        const int CANNON_TO = 4;
        const int KNIGHT_FROM = 5;
        const int KNIGHT_TO = 6;
        const int PAWN_FROM = 7;
        const int PAWN_TO = 11;
        const int BISHOP_FROM = 12;
        const int BISHOP_TO = 13;
        const int GUARD_FROM = 14;
        const int GUARD_TO = 15;

        public const int RANK_TOP = 3;
        public const int RANK_BOTTOM = 12;
        public const int FILE_LEFT = 3;
        public const int FILE_CENTER = 7;
        public const int FILE_RIGHT = 11;

        /* 棋子序号对应的棋子类型，带颜色
         *
         * ElephantEye的棋子序号从0到47，其中0到15不用，16到31表示红子，32到47表示黑子。
         * 每方的棋子顺序依次是：帅车车炮炮马马兵兵兵兵兵相相仕仕(将车车炮炮马马卒卒卒卒卒象象士士)
         * 提示：判断棋子是红子用"pc < 32"，黑子用"pc >= 32"
         */
        public readonly static int[] cnPieceTypes = {
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          17,18,18,19,19,20,20,21,21,21,21,21,22,22,23,23,
          33,34,34,35,35,36,36,37,37,37,37,37,38,38,39,39
        };
        // 棋子序号对应的棋子类型，不带颜色
        public readonly static int[] cnPieceKinds = {
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5, 5, 6, 6, 7, 7,
          1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5, 5, 6, 6, 7, 7,
        };

        //Interface to graphic board. x, y is 0~9
        public int UI_GetPiece(int x, int y)
        {
            return pcSquares[XY2Coord(x + FILE_LEFT, y + RANK_TOP)];
        }

        //Interface to graphic board. x, y is 0~9
        public int UI_GetFlippedPiece(int x, int y)
        {
            x = FILE_FLIP(x + FILE_LEFT);
            y = RANK_FLIP(y + RANK_TOP);
            return pcSquares[XY2Coord(x, y)];
        }

        //Interface to graphic board. x, y is 0~9
        public static int UI_XY2Coord(int x, int y)
        {
            return XY2Coord(x + FILE_LEFT, y + RANK_TOP);
        }

        public static System.Drawing.Point UI_Coord2XY(int sq, bool flipped)
        {
            if (flipped)
                sq = SQUARE_FLIP(sq);
            return new System.Drawing.Point(FILE_X(sq) - FILE_LEFT, RANK_Y(sq) - RANK_TOP);
        }

        public static int SQUARE_FLIP(int sq)
        {
            return 254 - sq;
        }

        public static int FILE_FLIP(int x)
        {
            return 14 - x;
        }

        public static int RANK_FLIP(int y)
        {
            return 15 - y;
        }

        /* ElephantEye的很多代码中都用到"SIDE_TAG()"这个量，红方设为16，黑方设为32。
         * 用"SIDE_TAG() + i"可以方便地选择棋子的类型，"i"从0到15依次是：
         * 帅车车炮炮马马相相仕仕兵兵兵兵兵(将车车炮炮马马卒卒卒卒卒象象士士)
         * 例如"i"取"KNIGHT_FROM"到"KNIGHT_TO"，则表示依次检查两个马的位置
         */
        public static int SIDE_TAG(int sd)
        {
            return (sd > 0 ? 32 : 16);
        }

        public static int OPP_SIDE_TAG(int sd)
        {
            return (sd > 0 ? 16 : 32);
        }

        public static int INDEX(int pc)
        {
            return pc & 15;
        }

        public static int SIDE(int pc)
        {
            return (pc >> 4) - 1;
        }

        public static int XY2Coord(int x, int y)
        {
            return x + (y << 4);
        }

        public static int FILE_X(int sq)
        {
            return sq & 15;
        }

        public static int RANK_Y(int sq)
        {
            return sq >> 4;
        }

        void ClearBoard()
        { // 棋盘初始化
            sdPlayer = 0;
            Array.Clear(pcSquares, 0, 256);
            Array.Clear(sqPieces, 0, 48);
        }
    }
};

