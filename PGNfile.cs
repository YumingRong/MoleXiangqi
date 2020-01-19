using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace XQWizardLight
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

        Position posStart;
        static readonly string[] cszResult = {  "*", "1-0", "1/2-1/2", "0-1"};
        bool Read(string szFileName) 
        {
            Position pos = new Position();
            pos.FromFEN(Position.cszStartFen);
            PgnFileStruct pgnFile;
            using (StreamReader fp = new StreamReader(szFileName))
            {
                pos = posStart;
                while (fp.Peek()>-1)
                {
                    string line = fp.ReadLine();
                    string pattern = @"\A\[(\S+)\s\u0022(\S+)\u0022\]\Z";
                    System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(line, pattern);
                    if (m.Success)
                    {
                        switch (m.Groups[0].Value)
                        {
                            case "Result":
                                pgnFile.nResult = 0;
                                break;
                        }

                    }
                }
            }

          return true;
        }


    }
}
