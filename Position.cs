using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        public ulong Key;
        int HalfMoveClock;

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
            Debug.Assert(IN_BOARD[sq]);
            if (flipped)
                sq = SQUARE_FLIP(sq);
            return new System.Drawing.Point(FILE_X(sq) - FILE_LEFT, RANK_Y(sq) - RANK_TOP);
        }

        public static int SQUARE_FLIP(int sq)
        {
            Debug.Assert(IN_BOARD[sq]);
            return 254 - sq;
        }

        public static int FILE_FLIP(int x)
        {
            Debug.Assert(x >= 0 && x < 16);
            return 14 - x;
        }

        public static int RANK_FLIP(int y)
        {
            Debug.Assert(y >= 0 && y < 16);
            return 15 - y;
        }

        /* ElephantEye的很多代码中都用到"SIDE_TAG()"这个量，红方设为16，黑方设为32。
         * 用"SIDE_TAG() + i"可以方便地选择棋子的类型，"i"从0到15依次是：
         * 帅车车炮炮马马相相仕仕兵兵兵兵兵(将车车炮炮马马卒卒卒卒卒象象士士)
         * 例如"i"取"KNIGHT_FROM"到"KNIGHT_TO"，则表示依次检查两个马的位置
         */
        public static int SIDE_TAG(int sd)
        {
            Debug.Assert(sd == 0 || sd == 1);
            return (sd > 0 ? 32 : 16);
        }

        public static int OPP_SIDE_TAG(int sd)
        {
            Debug.Assert(sd == 0 || sd == 1);
            return (sd > 0 ? 16 : 32);
        }

        public static int INDEX(int pc)
        {
            Debug.Assert(pc < 48);
            return pc & 15;
        }

        public static int SIDE(int pc)
        {
            Debug.Assert(pc < 48);
            return (pc >> 4) - 1;
        }

        public static int XY2Coord(int x, int y)
        {
            Debug.Assert(x >= 0 && x < 16);
            Debug.Assert(y >= 0 && y < 16);
            return x + (y << 4);
        }

        public static int FILE_X(int sq)
        {
            //Debug.Assert(IN_BOARD[sq]);
            return sq & 15;
        }

        public static int RANK_Y(int sq)
        {
            //Debug.Assert(IN_BOARD[sq]);
            return sq >> 4;
        }

        void ClearBoard()
        { // 棋盘初始化
            sdPlayer = 0;
            Array.Clear(pcSquares, 0, 256);
            Array.Clear(sqPieces, 0, 48);
        }

        bool BoardIsOK()
        {
            for (int pc = 16; pc < 48; pc++)
            {
                int sq = sqPieces[pc];
                if (sq > 0 && pcSquares[sq] != pc)
                    return false;
            }
            return true;
        }

        // 该数组很方便地实现了坐标的镜像(左右对称)
        public static readonly int[] csqMirrorTab = {
          0, 0, 0,    0,    0,    0,    0,    0,    0,    0,    0,    0, 0, 0, 0, 0,
          0, 0, 0,    0,    0,    0,    0,    0,    0,    0,    0,    0, 0, 0, 0, 0,
          0, 0, 0,    0,    0,    0,    0,    0,    0,    0,    0,    0, 0, 0, 0, 0,
          0, 0, 0, 0x3b, 0x3a, 0x39, 0x38, 0x37, 0x36, 0x35, 0x34, 0x33, 0, 0, 0, 0,
          0, 0, 0, 0x4b, 0x4a, 0x49, 0x48, 0x47, 0x46, 0x45, 0x44, 0x43, 0, 0, 0, 0,
          0, 0, 0, 0x5b, 0x5a, 0x59, 0x58, 0x57, 0x56, 0x55, 0x54, 0x53, 0, 0, 0, 0,
          0, 0, 0, 0x6b, 0x6a, 0x69, 0x68, 0x67, 0x66, 0x65, 0x64, 0x63, 0, 0, 0, 0,
          0, 0, 0, 0x7b, 0x7a, 0x79, 0x78, 0x77, 0x76, 0x75, 0x74, 0x73, 0, 0, 0, 0,
          0, 0, 0, 0x8b, 0x8a, 0x89, 0x88, 0x87, 0x86, 0x85, 0x84, 0x83, 0, 0, 0, 0,
          0, 0, 0, 0x9b, 0x9a, 0x99, 0x98, 0x97, 0x96, 0x95, 0x94, 0x93, 0, 0, 0, 0,
          0, 0, 0, 0xab, 0xaa, 0xa9, 0xa8, 0xa7, 0xa6, 0xa5, 0xa4, 0xa3, 0, 0, 0, 0,
          0, 0, 0, 0xbb, 0xba, 0xb9, 0xb8, 0xb7, 0xb6, 0xb5, 0xb4, 0xb3, 0, 0, 0, 0,
          0, 0, 0, 0xcb, 0xca, 0xc9, 0xc8, 0xc7, 0xc6, 0xc5, 0xc4, 0xc3, 0, 0, 0, 0,
          0, 0, 0,    0,    0,    0,    0,    0,    0,    0,    0,    0, 0, 0, 0, 0,
          0, 0, 0,    0,    0,    0,    0,    0,    0,    0,    0,    0, 0, 0, 0, 0,
          0, 0, 0,    0,    0,    0,    0,    0,    0,    0,    0,    0, 0, 0, 0, 0,
        };
    }
};

