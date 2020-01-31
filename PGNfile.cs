using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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

    }

    partial class POSITION
    {
        private Dictionary<char, int> PieceDict;
        private Dictionary<char, int> NumberDict;


        public void InitPGN()
        {
            PieceDict = new Dictionary<char, int>();
            NumberDict = new Dictionary<char, int>();
            FillDictionary();
            iMoveList = new List<iMOVE>();
        }

        public struct PgnFileStruct
        {
            public string Event, Round, Date, Site;
            public string RedTeam, Red, RedElo;
            public string BlackTeam, Black, BlackElo;
            public string ECCO, Opening, Variation, Result;
            public string Format, StartFEN;
        }

        public PgnFileStruct PGN;
        public List<iMOVE> iMoveList;

        public bool ReadPgnFile(string szFileName)
        {
            FromFEN(cszStartFen);
            PGN.StartFEN = cszStartFen;
            iMoveList.Clear();

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
                                PGN.Format = m.Groups[2].Value;
                                break;
                            case "Game":
                                if (m.Groups[2].Value != "Chinese Chess")
                                    return false;
                                break;
                            case "Event":
                                PGN.Event = m.Groups[2].Value;
                                break;
                            case "Date":
                                PGN.Date = m.Groups[2].Value;
                                break;
                            case "Round":
                                PGN.Round = m.Groups[2].Value;
                                break;
                            case "FEN":
                                FromFEN(m.Groups[2].Value);
                                PGN.StartFEN = m.Groups[2].Value;
                                break;
                            case "Result":
                                PGN.Result = m.Groups[2].Value;
                                break;
                            case "Site":
                                PGN.Site = m.Groups[2].Value;
                                break;
                            case "RedTeam":
                                PGN.RedTeam = m.Groups[2].Value;
                                break;
                            case "BlackTeam":
                                PGN.BlackTeam = m.Groups[2].Value;
                                break;
                            case "Red":
                                PGN.Red = m.Groups[2].Value;
                                break;
                            case "Black":
                                PGN.Black = m.Groups[2].Value;
                                break;
                            case "ECCO":
                                PGN.ECCO = m.Groups[2].Value;
                                break;
                            case "Opening":
                                PGN.Opening = m.Groups[2].Value;
                                break;
                            case "Variation":
                                PGN.Variation = m.Groups[2].Value;
                                break;
                            case "BlackElo":
                                PGN.BlackElo = m.Groups[2].Value;
                                break;
                            case "RedElo":
                                PGN.RedElo = m.Groups[2].Value;
                                break;
                            default:
                                Debug.WriteLine("Unknown label {0}", m.Groups[2].Value);
                                break;
                        }
                    }
                    else
                    {
                        //Now comes the move and comment list
                        line += "\n" + fp.ReadToEnd();
                    }
                }
                int index;
                iMOVE imv = new iMOVE();
                //int phase = 2; //phase = 0是序号，1是move#1，2是 move#2
                do
                {
                    //get move list till next comment
                    index = line.IndexOf('{');
                    string content;
                    if (index < 0)
                        content = line;
                    else if (index == 0)
                    {
                        int index1 = line.IndexOf('}');
                        if (index1 > 0)
                        {//is comment
                            imv.comment = line.Substring(1, index1 - 1);
                            line = line.Substring(index1 + 1);
                        }
                        continue;
                    }
                    else
                    {
                        content = line.Substring(0, index);
                        line = line.Substring(index);
                    }
                    string[] words = content.Split(new char[] { ' ', '\n', '\r', '.' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in words)
                    {
                        pattern = @"\A(\d+)\Z";
                        m = Regex.Match(s, pattern);
                        if (m.Success)
                        {//is a move number
                            //Debug.WriteLine(m.Groups[1].Value);
                            //if (phase != 2 && m.Groups[1].Value != "2")//第一回合如果黑方先走，只有move#2
                            //{
                            //    Console.WriteLine("棋谱错误，缺少着法");
                            //    return false;
                            //}
                            //phase = 0;
                        }
                        else if (s.Length == 4)
                        {//is a move
                            //Debug.WriteLine(s);
                            MOVE mv = ParseWord(s);
                            if (mv.pcSrc == 0)
                                return false;
                            MakeMove(mv);
                            iMoveList.Add(imv);
                            imv = new iMOVE();
                            imv.from = mv.sqSrc;
                            imv.to = mv.sqDst;
                            //phase++;
                        }
                        else
                        {
                            if (s != PGN.Result)
                                Debug.WriteLine(s);
                            iMoveList.Add(imv);
                            return true;
                        }
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
                if (t == null)
                    return mv;
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
                    pc = i + SIDE_TAG(sdPlayer);
                    sq = sqPieces[pc];
                    if (sq > 0)
                    {
                        x = FILE_X(sq);
                        pc_x[x]++;
                        if (pc_x[x] > 1)
                        {
                            file0 = x;
                            break;
                        }
                    }
                }
                int dir = 1;
                if (word[0] == '前')
                    dir = 1;
                else if (word[0] == '后')
                    dir = -1;
                if (sdPlayer == 1)
                    dir = -dir;
                if (dir > 0)
                    y = RANK_TOP;
                else
                    y = RANK_BOTTOM;
                do
                {
                    sq = XY2Coord(x, y);
                    pc = pcSquares[sq];
                    y += dir;
                }
                while (cnPieceTypes[pc] != pcType + SIDE_TAG(sdPlayer));
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
                if (sdPlayer == 0)
                    dir = -dir;
                if (pcType == PIECE_KNIGHT || pcType == PIECE_BISHOP || pcType == PIECE_GUARD)
                {
                SecondHalf:
                    int rank0 = RANK_Y(mv.sqSrc);
                    int rank1;
                    file1 = FindFile(word[3]);
                    if (pcType == PIECE_KNIGHT)
                        rank1 = rank0 + (3 - Math.Abs(file1 - file0)) * dir;
                    else if (pcType == PIECE_BISHOP)
                        rank1 = rank0 + 2 * dir;
                    else //PIECE_GUARD
                        rank1 = rank0 + dir;
                    mv.sqDst = XY2Coord(file1, rank1);
                    if (!LegalMove(mv.sqSrc, mv.sqDst))
                    {//有些棋谱会出现相、仕在同一列不用前后表示的情况
                        //Debug.WriteLine("不规范的记谱:" + word);
                        mv.pcSrc++;
                        mv.sqSrc = sqPieces[mv.pcSrc];
                        Debug.Assert(FILE_X(sqPieces[mv.pcSrc]) == file0);
                        goto SecondHalf;
                    }
                }
                else
                {
                    //rook, cannon, king or pawn
                    mv.sqDst = mv.sqSrc + 16 * FindRank(word[3]) * dir;
                }
            }

            mv.pcDst = pcSquares[mv.sqDst];
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
        int[] pcFrom = { 0, ROOK_FROM, CANNON_FROM, KNIGHT_FROM, PAWN_FROM, KING_FROM, BISHOP_FROM, GUARD_FROM };
        int[] pcTo = { 0, ROOK_TO, CANNON_TO, KNIGHT_TO, PAWN_TO, KING_TO, BISHOP_TO, GUARD_TO };

        Tuple<int, int> FindPiece(int pcType, int file)
        {
            for (int i = pcFrom[pcType]; i <= pcTo[pcType]; i++)
            {
                int pc = i + SIDE_TAG(sdPlayer);
                int sq = sqPieces[pc];
                if (FILE_X(sq) == file)
                    return Tuple.Create(pc, sq);
            }
            Debug.WriteLine("Cannot find the piece in the file");
            return null;
        }

        void FillDictionary()
        {
            PieceDict.Add('车', PIECE_ROOK);
            PieceDict.Add('炮', PIECE_CANNON);
            PieceDict.Add('马', PIECE_KNIGHT);
            PieceDict.Add('兵', PIECE_PAWN);
            PieceDict.Add('卒', PIECE_PAWN);
            PieceDict.Add('帅', PIECE_KING);
            PieceDict.Add('将', PIECE_KING);
            PieceDict.Add('相', PIECE_BISHOP);
            PieceDict.Add('象', PIECE_BISHOP);
            PieceDict.Add('仕', PIECE_GUARD);
            PieceDict.Add('士', PIECE_GUARD);
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

        /// <summary>
        /// 写入数据到CSV文件，覆盖形式
        /// </summary>
        /// <param name="csvPath">要写入的字符串表示的CSV文件</param>
        /// <param name="LineDataList">要写入CSV文件的数据</param>
        public static void Write2Csv(string csvPath, int[,] array)
        {
            using (FileStream fs = new FileStream(csvPath.Trim(), FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                {
                    sw.AutoFlush = false;
                    for(int sd = 0;sd<2;sd++)
                    {
                        for (int y = RANK_TOP; y <= RANK_BOTTOM; y++)//<--row
                        {
                            for (int x = FILE_LEFT; x <= FILE_RIGHT; x++)//<--col
                            {
                                sw.Write(string.Format("{0},", array[sd, XY2Coord(x, y)]));
                            }
                            sw.Write('\n');
                        }
                        sw.WriteLine();
                    }
                    sw.Flush();
                }
            }
        }

    }
}
