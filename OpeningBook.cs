using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MoleXiangqi
{
    [Serializable]
    public struct BookEntry: ISerializable
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

    public class OpeningDictionary
    {
        Dictionary<ulong, BookEntry> Book = new Dictionary<ulong, BookEntry>();

        public void BuildBook(string pgn_path, string bookfile, int maxHeight)
        {
            BatchReadPGN(pgn_path, maxHeight);
            using (FileStream fileStream = new FileStream(bookfile, FileMode.OpenOrCreate,FileAccess.ReadWrite))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, Book);
            }
            Console.WriteLine("Book is saved. ");
        }

        public void ReadTest(string fileName)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
            {
                IFormatter formatter = new BinaryFormatter();
                Book = (Dictionary<ulong, BookEntry>)(formatter.Deserialize(fileStream));
            }
            Console.WriteLine("Book is loaded. ");
        }

        // 该数组很方便地实现了坐标的镜像(左右对称)
        static readonly int[] csqMirrorTab = {
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
                mirror_mv.sqSrc = csqMirrorTab[mv.sqSrc];
                mirror_mv.sqDst = csqMirrorTab[mv.sqDst];
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
