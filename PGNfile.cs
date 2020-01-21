using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace MoleXiangqi
{
    class PGNfile
    {
        Position pos;
        Dictionary<Char, int> PieceDict, NumberDict;

        public PGNfile()
        {
            pos = new Position();
            PieceDict = new Dictionary<char, int>();
            NumberDict = new Dictionary<char, int>();
            FillDictionary();
        }


        struct PgnFileStruct
        {
            public string Event, Round, Date, Site;
            public string RedTeam, Red, RedElo;
            public string BlackTeam, Black, BlackElo;
            public string ECCO, Opening, Variation, Result;
            public string Format;
            public MOVE[] MoveList;
            public string[] CommentList;
        }


        //static readonly string[] cszResult = {  "*", "1-0", "1/2-1/2", "0-1"};
        public bool Read(string szFileName)
        {
            pos.FromFEN(Position.cszStartFen);
            PgnFileStruct pgnFile;
            const int nMaxMove = 300;
            pgnFile.MoveList = new MOVE[nMaxMove];
            pgnFile.CommentList = new string[nMaxMove];

            int nStep = 0;
            using (StreamReader fp = new StreamReader(szFileName, Encoding.GetEncoding("GB2312")))
            {
                string pattern = @"\A\[(\w+)\s""(.*)""\]\Z";
                Regex reg = new Regex(pattern);
                Match m;
                string line = "";
                while (fp.Peek() > -1)
                {
                    line = fp.ReadLine();
                    m = reg.Match(line);
                    if (m.Success)
                    {
                        switch (m.Groups[1].Value)
                        {
                            case "Format":
                                pgnFile.Format = m.Groups[2].Value;
                                break;
                            case "Game":
                                if (m.Groups[2].Value != "Chinese Chess")
                                    return false;
                                break;
                            case "Event":
                                pgnFile.Event = m.Groups[2].Value;
                                break;
                            case "Date":
                                pgnFile.Date = m.Groups[2].Value;
                                break;
                            case "Round":
                                pgnFile.Round = m.Groups[2].Value;
                                break;
                            case "FEN":
                                pos.FromFEN(m.Groups[2].Value);
                                break;
                            case "Result":
                                pgnFile.Result = m.Groups[2].Value;
                                break;
                            case "Site":
                                pgnFile.Site = m.Groups[2].Value;
                                break;
                            case "RedTeam":
                                pgnFile.RedTeam = m.Groups[2].Value;
                                break;
                            case "BlackTeam":
                                pgnFile.BlackTeam = m.Groups[2].Value;
                                break;
                            case "Red":
                                pgnFile.Red = m.Groups[2].Value;
                                break;
                            case "Black":
                                pgnFile.Black = m.Groups[2].Value;
                                break;
                            case "ECCO":
                                pgnFile.ECCO = m.Groups[2].Value;
                                break;
                            case "Opening":
                                pgnFile.Opening = m.Groups[2].Value;
                                break;
                            case "Variation":
                                pgnFile.Variation = m.Groups[2].Value;
                                break;
                            case "BlackElo":
                                pgnFile.BlackElo = m.Groups[2].Value;
                                break;
                            case "RedElo":
                                pgnFile.RedElo = m.Groups[2].Value;
                                break;
                            default:
                                Console.WriteLine("Unknown label {0}", m.Groups[2].Value);
                                break;
                        }
                    }
                    else
                    {
                        //Now comes the move and comment list
                        line += fp.ReadToEnd();
                    }
                }
                int index;
                do
                {
                    //get move list till next comment
                    index = line.IndexOf('{');
                    string content = "";
                    if (index > 0)
                    {
                        content = line.Substring(0, index);
                        line = line.Substring(index);
                    }
                    string[] words = content.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in words)
                    {
                        if (s.Length == 4)
                        {//is a move
                            Console.WriteLine(s);
                            MOVE mv = ParseWord(s);
                            pos.MakeMove(mv);
                        }
                        else
                        {
                            pattern = @"(\d+)\.";
                            m = Regex.Match(s, pattern);
                            if (m.Success)
                            {//is a move number
                                nStep = Convert.ToInt32(m.Groups[1].Value);
                                Console.WriteLine(nStep);
                            }
                        }
                    }
                    Regex commentReg = new Regex(@"\{(.*)\}");
                    m = commentReg.Match(line);
                    if (m.Success)
                    {//is comment
                        pgnFile.CommentList[nStep] = m.Groups[1].Value;
                        line = line.Substring(m.Groups[1].Value.Length + 2);
                    }

                } while (line.Length > 0 & index >= 0);
            }

            return true;
        }

        MOVE ParseWord(string word)
        {
            MOVE mv = new MOVE();
            int pcType, sq, file0 = 0, file1;

            if (PieceDict.TryGetValue(word[0], out pcType))
            {//Normal case 炮八平五
                file0 = FindFile(word[1]);
                Tuple<int, int> t = FindPiece(pcType, file0);
                mv.pcSrc = t.Item1;
                mv.sqSrc = t.Item2;
            }
            else
            {//special case 前马退二
                int[] pc_x = new int[16];
                pcType = PieceDict[word[1]];
                int x = 0, y, pc;
                //find doubled pieces on the same file
                for (int i = pcFrom[pcType]; i <= pcTo[pcType]; i++)
                {
                    pc = i + Position.SIDE_TAG(pos.sdPlayer);
                    sq = pos.sqPieces[pc];
                    x = Position.FILE_X(sq);
                    pc_x[x]++;
                    if (pc_x[x] > 1)
                    {
                        file0 = x;
                        break;
                    }
                }

                int dir = 1;
                if (word[0] == '前')
                    dir = 1;
                else if (word[0] == '后')
                    dir = -1;
                if (pos.sdPlayer == 1)
                    dir = -dir;
                if (dir > 0)
                    y = Position.RANK_TOP;
                else
                    y = Position.RANK_BOTTOM;
                do
                {
                    sq = Position.XY2Coord(x, y);
                    pc = pos.pcSquares[sq];
                    y += dir;
                }
                while (Position.cnPieceTypes[pc] != pcType + Position.SIDE_TAG(pos.sdPlayer));
                mv.sqSrc = sq;
                mv.pcSrc = pc;
            }
            if (word[2] == '平')
            {
                file1 = FindFile(word[3]);
                mv.sqDst = mv.sqSrc + file1 - file0;
            }
            else
            {
                int dir = 1;
                if (word[2] == '进')
                    dir = 1;
                else if (word[2] == '退')
                    dir = -1;
                else
                    Debug.Fail("Unrecoginized move 2 {0}", word);
                if (pos.sdPlayer == 0)
                    dir = -dir;
                if (pcType == 3 || pcType == 6 || pcType == 7)
                {//knight, bishop or guard
                    int rank0 = Position.RANK_Y(mv.sqSrc);
                    int rank1;
                    file1 = FindFile(word[3]);
                    if (pcType == 3)
                        rank1 = rank0 + (3 - Math.Abs(file1 - file0)) * dir;
                    else if (pcType == 6)
                        rank1 = rank0 + 2 * dir;
                    else
                        rank1 = rank0 + dir;
                    mv.sqDst = Position.XY2Coord(file1, rank1);
                }
                else
                {//rook, cannon, king or pawn
                    mv.sqDst = mv.sqSrc + 16 * FindRank(word[3]) * dir;
                }
            }

            mv.pcDst = pos.pcSquares[mv.sqDst];
            return mv;
        }

        int FindFile(Char c)
        {
            int file;
            if (Char.IsDigit(c))
                file = (int)Char.GetNumericValue(c) + 2;
            else
                file = 12 - NumberDict[c];
            return file;
        }

        int FindRank(Char c)
        {
            int rank;
            if (Char.IsDigit(c))
                rank = (int)Char.GetNumericValue(c);
            else
                rank = NumberDict[c];
            return rank;
        }

        // 每种子力的开始序号和结束序号
        int[] pcFrom = { 0, 0, 2, 4, 6, 11, 12, 14 };
        int[] pcTo = { 0, 1, 3, 5, 10, 11, 13, 15 };

        Tuple<int, int> FindPiece(int pcType, int file)
        {
            for (int i = pcFrom[pcType]; i <= pcTo[pcType]; i++)
            {
                int pc = i + Position.SIDE_TAG(pos.sdPlayer);
                int sq = pos.sqPieces[pc];
                if (Position.FILE_X(sq) == file)
                    return Tuple.Create(pc, sq);
            }
            Debug.Fail("Cannot find the piece in the file");
            return null;
        }

        void FillDictionary()
        {
            PieceDict.Add('车', 1);
            PieceDict.Add('炮', 2);
            PieceDict.Add('马', 3);
            PieceDict.Add('兵', 4);
            PieceDict.Add('卒', 4);
            PieceDict.Add('帅', 5);
            PieceDict.Add('将', 5);
            PieceDict.Add('相', 6);
            PieceDict.Add('象', 6);
            PieceDict.Add('仕', 7);
            PieceDict.Add('士', 7);
            NumberDict.Add('一', 1);
            NumberDict.Add('二', 2);
            NumberDict.Add('三', 3);
            NumberDict.Add('四', 4);
            NumberDict.Add('五', 5);
            NumberDict.Add('六', 6);
            NumberDict.Add('七', 7);
            NumberDict.Add('八', 8);
            NumberDict.Add('九', 9);

        }




        //        /* "File2Move()"函数将纵线符号表示转换为内部着法表示。
        //         *
        //         * 这个函数以及后面的"Move2File()"函数是本模块最难处理的两个函数，特别是在处理“两条的纵线上有多个兵(卒)”的问题上。
        //         * 在棋谱的快速时，允许只使用数字键盘，因此1到7依次代表帅(将)到兵(卒)这七种棋子，"File2Move()"函数也考虑到了这个问题。
        //         */
        //        int File2Move(int dwFileStr,  CPosition &pos) {
        //  int i, j, nPos, pt, sq, nPieceNum;
        //        int xSrc, ySrc, xDst, yDst;
        //        C4dwStruct FileStr;
        //        int nFileList[9], nPieceList[5];
        //        // 纵线符号表示转换为内部着法表示，通常分为以下几个步骤：

        //        // 1. 检查纵线符号是否是仕(士)相(象)的28种固定纵线表示，在这之前首先必须把数字、小写等不统一的格式转换为统一格式；
        //        FileStr.dw = dwFileStr;
        //  switch (FileStr.c[0]) {
        //  case '2':
        //  case 'a':
        //    FileStr.c[0] = 'A';
        //    break;
        //  case '3':
        //  case 'b':
        //  case 'E':
        //  case 'e':
        //    FileStr.c[0] = 'B';
        //    break;
        //  default:
        //    break;
        //  }
        //  if (FileStr.c[3] == 'p') {
        //    FileStr.c[3] = 'P';
        //  }
        //  for (i = 0; i<MAX_FIX_FILE; i ++) {
        //    if (FileStr.dw == cdwFixFile[i]) {
        //      if (pos.sdPlayer == 0) {
        //        return MOVE(cucFixMove[i][0], cucFixMove[i][1]);
        //      } else {
        //        return MOVE(SQUARE_FLIP(cucFixMove[i][0]), SQUARE_FLIP(cucFixMove[i][1]));
        //      }
        //    }
        //  }

        //  // 2. 如果不是这28种固定纵线表示，那么把棋子、位置和纵线序号(列号)解析出来
        //  nPos = Byte2Direct(FileStr.c[0]);
        //  if (nPos == MAX_DIRECT) {
        //    pt = Byte2Piece(FileStr.c[0]);
        //nPos = Byte2Pos(FileStr.c[1]);
        //  } else {
        //    pt = Byte2Piece(FileStr.c[1]);
        //nPos += DIRECT_TO_POS;
        //  }
        //  if (nPos == MAX_POS) {

        //    // 3. 如果棋子是用列号表示的，那么可以直接根据纵线来找到棋子序号；
        //    xSrc = Byte2Digit(FileStr.c[1]);
        //    if (pt == KING_TYPE) {
        //      sq = FILESQ_SIDE_PIECE(pos, 0);
        ////    } else if (pt >= KNIGHT_TYPE && pt <= PAWN_TYPE) {
        //    } else if (pt >= ROOK_TYPE && pt <= PAWN_TYPE) {
        //      j = (pt == PAWN_TYPE? 5 : 2);
        //      for (i = 0; i<j; i ++) {
        //        sq = FILESQ_SIDE_PIECE(pos, FIRST_PIECE(pt, i));
        //        if (sq != -1) {
        //          if (FILESQ_FILE_X(sq) == xSrc) {
        //            break;
        //          }
        //        }
        //      }
        //      sq = (i == j? -1 : sq);
        //    } else {
        //      sq = -1;
        //    }
        //  } else {

        //    // 4. 如果棋子是用位置表示的，那么必须挑选出含有多个该种棋子的所有纵线，这是本函数最难处理的地方；
        ////    if (pt >= KNIGHT_TYPE && pt <= PAWN_TYPE) {
        //    if (pt >= ROOK_TYPE && pt <= PAWN_TYPE) {
        //      for (i = 0; i< 9; i ++) {
        //        nFileList[i] = 0;
        //      }
        //      j = (pt == PAWN_TYPE? 5 : 2);
        //      for (i = 0; i<j; i ++) {
        //        sq = FILESQ_SIDE_PIECE(pos, FIRST_PIECE(pt, i));
        //        if (sq != -1) {
        //          nFileList[FILESQ_FILE_X(sq)] ++;
        //        }
        //      }
        //      nPieceNum = 0;
        //      for (i = 0; i<j; i ++) {
        //        sq = FILESQ_SIDE_PIECE(pos, FIRST_PIECE(pt, i));
        //        if (sq != -1) {
        //          if (nFileList[FILESQ_FILE_X(sq)] > 1) {
        //            nPieceList[nPieceNum] = FIRST_PIECE(pt, i);
        //nPieceNum ++;
        //          }
        //        }
        //      }

        //      // 5. 找到这些纵线以后，对这些纵线上的棋子进行排序，然后根据位置来确定棋子序号；
        //      for (i = 0; i<nPieceNum - 1; i ++) {
        //        for (j = nPieceNum - 1; j > i; j --) {
        //          if (FILESQ_SIDE_PIECE(pos, nPieceList[j - 1]) > FILESQ_SIDE_PIECE(pos, nPieceList[j])) {
        //            SWAP(nPieceList[j - 1], nPieceList[j]);
        //          }
        //        }
        //      }
        //      // 提示：如果只有两个棋子，那么“后”表示第二个棋子，如果有多个棋子，
        //      // 那么“一二三四五”依次代表第一个到第五个棋子，“前中后”依次代表第一个到第三个棋子。
        //      if (nPieceNum == 2 && nPos == 2 + DIRECT_TO_POS) {
        //        sq = FILESQ_SIDE_PIECE(pos, nPieceList[1]);
        //      } else {
        //        nPos -= (nPos >= DIRECT_TO_POS? DIRECT_TO_POS : 0);
        //        sq = (nPos >= nPieceNum? -1 : FILESQ_SIDE_PIECE(pos, nPieceList[nPos]));
        //      }
        //    } else {
        //      sq = -1;
        //    }
        //  }
        //  if (sq == -1) {
        //    return 0;
        //  }

        //  // 6. 现在已知了着法的起点，就可以根据纵线表示的后两个符号来确定着法的终点；
        //  xSrc = FILESQ_FILE_X(sq);
        //ySrc = FILESQ_RANK_Y(sq);
        //  if (pt == KNIGHT_TYPE) {
        //    // 提示：马的进退处理比较特殊。
        //    xDst = Byte2Digit(FileStr.c[3]);
        //    if (FileStr.c[2] == '+') {
        //      yDst = ySrc - 3 + abs(xDst - xSrc);
        //    } else {
        //      yDst = ySrc + 3 - abs(xDst - xSrc);
        //    }
        //  } else {
        //    if (FileStr.c[2] == '+') {
        //      xDst = xSrc;
        //      yDst = ySrc - Byte2Digit(FileStr.c[3]) - 1;
        //    } else if (FileStr.c[2] == '-') {
        //      xDst = xSrc;
        //      yDst = ySrc + Byte2Digit(FileStr.c[3]) + 1;
        //    } else {
        //      xDst = Byte2Digit(FileStr.c[3]);
        //yDst = ySrc;
        //    }
        //  }
        //  // 注意：yDst有可能超过范围！
        //  if (yDst< 0 || yDst> 9) {
        //    return 0;
        //  }

        //  // 7. 把相对走子方的坐标转换为固定坐标，得到着法的起点和终点。
        //  if (pos.sdPlayer == 0) {
        //    return MOVE(FILESQ_SQUARE(FILESQ_COORD_XY(xSrc, ySrc)), FILESQ_SQUARE(FILESQ_COORD_XY(xDst, yDst)));
        //  } else {
        //    return MOVE(SQUARE_FLIP(FILESQ_SQUARE(FILESQ_COORD_XY(xSrc, ySrc))),
        //        SQUARE_FLIP(FILESQ_SQUARE(FILESQ_COORD_XY(xDst, yDst))));
        //  }
        //}

    }
}
