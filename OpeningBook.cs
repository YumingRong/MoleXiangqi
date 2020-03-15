using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MoleXiangqi
{
    [Serializable]
    public struct BookEntry : ISerializable
    {
        public ushort win, loss, draw; //from the red side perspective

        // The special constructor is used to deserialize values.
        private BookEntry(SerializationInfo info, StreamingContext context)
        {
            win = info.GetUInt16("Win");
            draw = info.GetUInt16("Draw");
            loss = info.GetUInt16("Loss");
        }

        // Implement this method to serialize data. The method is called on serialization.
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Win", win);
            info.AddValue("Draw", draw);
            info.AddValue("Loss", loss);
        }
    }

    public class OpeningBook
    {
        Dictionary<ulong, BookEntry> Book = new Dictionary<ulong, BookEntry>();

        public bool TryGetValue(ulong key, out BookEntry entry)
        {
            return Book.TryGetValue(key, out entry);
        }

        public void BuildBook(string pgn_path, string bookfile, int maxHeight)
        {
            BatchReadPGN(pgn_path, maxHeight);
            using (FileStream fileStream = new FileStream(bookfile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, Book);
            }
            Console.WriteLine("Book is saved. ");
        }

        public void ReadBook(string fileName)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
            {
                IFormatter formatter = new BinaryFormatter();
                Book = (Dictionary<ulong, BookEntry>)(formatter.Deserialize(fileStream));
            }
            Console.WriteLine("Book is loaded. ");
        }

        //Read PGN files in a folder and store the opening into Book (in memory)
        void BatchReadPGN(string foldername, int maxHeight)
        {
            Debug.Assert(foldername != null);
            IEnumerable<string> pgnFiles = Directory.EnumerateFiles(foldername, "*.pgn", SearchOption.AllDirectories);
            int nFile = 0;
            int nSuccess = 0;
            Console.WriteLine($"Before building, book size is {Book.Count}.");
            foreach (string fileName in pgnFiles)
            {
                Console.WriteLine(fileName.Substring(foldername.Length + 1));
                nFile++;
                if (ReadPGN(fileName, maxHeight))
                    nSuccess++;
                else
                    Console.WriteLine("Fail to read!");
            }
            Console.WriteLine($"Read total {nFile} files. Pass {nSuccess}. Fail {nFile - nSuccess}. ");
            Console.WriteLine($"After building, book size is {Book.Count}.");
        }

        bool ReadPGN(string filename, int maxHeight)
        {
            POSITION pos = new POSITION();
            PgnFileStruct pgn = pos.ReadPgnFile(filename);
            if (pgn.StartFEN != POSITION.cszStartFen)
            {
                Console.WriteLine("非开局或全局谱");
                return false;
            }
            int result;
            switch (pgn.Result)
            {
                case "0-1":
                    result = -1;
                    break;
                case "1-0":
                    result = 1;
                    break;
                case "1/2-1/2":
                    result = 0;
                    break;
                default:
                    return false;
            }
            pos.FromFEN(pgn.StartFEN);
            POSITION mirror_pos = new POSITION();
            mirror_pos.FromFEN(pgn.StartFEN);

            int height = 0;
            foreach (MOVE mv in pgn.MoveList)
            {
                pos.MakeMove(mv, false);
                ulong key = pos.Key;

                MOVE mirror_mv = new MOVE();
                mirror_mv.sqSrc = POSITION.csqMirrorTab[mv.sqSrc];
                mirror_mv.sqDst = POSITION.csqMirrorTab[mv.sqDst];
                mirror_mv.pcSrc = mirror_pos.pcSquares[mirror_mv.sqSrc];
                mirror_mv.pcDst = mirror_pos.pcSquares[mirror_mv.sqDst];
                mirror_mv.checking = mv.checking;
                mirror_pos.MakeMove(mirror_mv, false);
                ulong mirror_key = mirror_pos.Key;

                if (Book.TryGetValue(key, out BookEntry entry))
                {
                    switch (result)
                    {
                        case 1:
                            entry.win++;
                            Debug.Assert(entry.win < ushort.MaxValue);
                            break;
                        case 0:
                            entry.draw++;
                            Debug.Assert(entry.loss < ushort.MaxValue);
                            break;
                        case -1:
                            entry.loss++;
                            Debug.Assert(entry.draw < ushort.MaxValue);
                            break;
                        default:
                            Debug.Fail("Unknown result");
                            break;
                    }
                    Book[key] = entry;
                }
                else if (Book.TryGetValue(mirror_key, out entry))
                {
                    switch (result)
                    {
                        case 1:
                            entry.win++;
                            Debug.Assert(entry.win < ushort.MaxValue);
                            break;
                        case 0:
                            entry.draw++;
                            Debug.Assert(entry.loss < ushort.MaxValue);
                            break;
                        case -1:
                            entry.loss++;
                            Debug.Assert(entry.draw < ushort.MaxValue);
                            break;
                        default:
                            Debug.Fail("Unknown result");
                            break;
                    }
                    Book[mirror_key] = entry;
                }
                else
                {
                    entry = new BookEntry();
                    switch (result)
                    {
                        case 1:
                            entry.win = 1;
                            break;
                        case 0:
                            entry.draw = 1;
                            break;
                        case -1:
                            entry.loss = 1;
                            break;
                        default:
                            Debug.Fail("Unknown result");
                            return false;
                    }
                    Book.Add(key, entry);
                }
                height++;
                if (height > maxHeight)
                    return true;
            }
            return true;
        }
    }
}
