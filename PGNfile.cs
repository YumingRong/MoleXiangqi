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
            public string ECCO, Open, Var;
            public int nMaxMove, nResult;
            public List<MOVE> MoveList;
            public List<string> CommentList;
        }

        static readonly string[] cszResult = {  "*", "1-0", "1/2-1/2", "0-1"};
        public bool Read(string szFileName) 
        {
            Position pos = new Position();
            pos.FromFEN(Position.cszStartFen);
            PgnFileStruct pgnFile;
            bool bLabel = true;
            using (StreamReader fp = new StreamReader(szFileName))
            {
                while (fp.Peek()>-1)
                {
                    string line;
                    if(bLabel)
                    {
                        line = fp.ReadLine();
                        string pattern = @"\A\[(\w+)\s""(.+)""\]\Z";
                        Regex reg = new Regex(pattern);
                        Match m = reg.Match(line);
                        //Match m = Regex.Match(line, pattern);
                        if (m.Success)
                        {
                            switch (m.Groups[1].Value)
                            {
                                case "Event":
                                    pgnFile.Event = m.Groups[2].Value;
                                    break;
                                case "FEN":
                                    pos.FromFEN(m.Groups[2].Value);
                                    break;
                                case "Result":
                                    pgnFile.nResult = 0;
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            bLabel = false;
                            line = fp.ReadLine();
                        }
                    }
                    else
                    {
                        //pattern = @"\d+\.\d*(\S+)\s+";
                        line = fp.ReadLine();
                    }
                }
            }

          return true;
        }


    }
}
