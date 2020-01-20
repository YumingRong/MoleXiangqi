using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MoleXiangqi
{
    class PGNfile
    {
        struct PgnFileStruct
        {
            public string Event, Round, Date, Site;
            public string RedTeam, Red, RedElo;
            public string BlackTeam, Black, BlackElo;
            public string ECCO, Opening, Variation, Result;
            public MOVE[] MoveList;
            public string[] CommentList;
        }

        //static readonly string[] cszResult = {  "*", "1-0", "1/2-1/2", "0-1"};
        public bool Read(string szFileName)
        {
            Position pos = new Position();
            pos.FromFEN(Position.cszStartFen);
            PgnFileStruct pgnFile;
            const int nMaxMove = 300;
            pgnFile.MoveList = new MOVE[nMaxMove];
            pgnFile.CommentList = new string[nMaxMove];

            int nStep = 0;
            using (StreamReader fp = new StreamReader(szFileName))
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
                        if (s.Length >= 6 || s.Length <= 8)
                        {//is a move
                            Console.WriteLine(s);
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


    }
}
