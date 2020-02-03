using System;
using System.Collections.Generic;
using System.IO;
using MathNet.Numerics.Statistics;
using System.Drawing;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoleXiangqi
{
    public partial class MainForm : Form
    {
        bool App_bSound = true;
        string App_szPath = @"J:\C#\MoleXiangqi\Resources\";
        bool App_inGame;
        Point mvLastFrom, mvLastTo, ptSelected;
        int pcSelected, pcLast;
        int FENStep; //用来决定要不要显示上一步方框
        bool bFlipped = false;
        bool bSelected;
        List<iMOVE> iMoveList;
        POSITION pos;
        const int gridSize = 57;
        SoundPlayer soundPlayer;
        readonly static int[] cnPieceImages = {
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5, 5, 6, 6, 7, 7,
          8, 9, 9,10,10,11,11,12,12,12,12,12,13,13,14,14
        };

        public MainForm()
        {
            InitializeComponent();
            pos = new POSITION();
            iMoveList = new List<iMOVE>();
            soundPlayer = new SoundPlayer();
        }

        private void NewGameMenu_Click(object sender, EventArgs e)
        {
            NewGame();
        }

        private void NewGame()
        {
            //swap side
            if (menuAIBlack.Checked)
            {
                menuAIRed.Checked = true;
                menuAIBlack.Checked = false;
                bFlipped = false;
            }
            else if (menuAIRed.Checked)
            {
                menuAIRed.Checked = false;
                menuAIBlack.Checked = true;
                bFlipped = true;
            }
            pos.FromFEN(POSITION.cszStartFen);
            NewFEN();
        }

        void NewFEN()
        {
            FENStep = 0;
            bSelected = false;
            listboxMove.Items.Clear();
            listboxMove.Items.Add("==开始==");
            iMoveList.Clear();
            iMoveList.Add(new iMOVE());
            panelBoard.Refresh();
            App_inGame = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            NewGame();
        }

        private void panelBoard_MouseClick(object sender, MouseEventArgs e)
        {
            if (!App_inGame)
                return;
            int x = e.X / gridSize;
            int y = e.Y / gridSize;

            if (x < 0 || x > 9 || y < 0 || y > 10)
                return;
            int piece;
            if (bFlipped)
                piece = pos.iGetFlippedPiece(x, y);
            else
                piece = pos.iGetPiece(x, y);

            Graphics g = panelBoard.CreateGraphics();
            if (bSelected)
            {
                if (POSITION.SIDE(piece) == pos.sdPlayer && ptSelected != new Point(x, y))
                //又选择别的己方子
                {
                    DrawBoard(ptSelected, g);
                    DrawPiece(ptSelected, pcSelected, g);
                    ptSelected = new Point(x, y);
                    pcSelected = cnPieceImages[piece];
                    DrawSelection(ptSelected, g);
                    PlaySound("CLICK");
                }
                else
                {
                    int sqFrom, sqTo;
                    if (bFlipped)
                    {
                        sqFrom = POSITION.iXY2Coord(8 - ptSelected.X, 9 - ptSelected.Y);
                        sqTo = POSITION.iXY2Coord(8 - x, 9 - y);
                    }
                    else
                    {
                        sqFrom = POSITION.iXY2Coord(ptSelected.X, ptSelected.Y);
                        sqTo = POSITION.iXY2Coord(x, y);
                    }
                    if (pos.LegalMove(sqFrom, sqTo))
                    {
                        if (FENStep > 0)
                        {
                            //擦除上一步的起始和结束位置选择框
                            DrawBoard(mvLastFrom, g);
                            DrawBoard(mvLastTo, g);
                            DrawPiece(mvLastTo, pcLast, g);
                        }

                        mvLastFrom = ptSelected;
                        mvLastTo = new Point(x, y);
                        pcLast = pcSelected;

                        //擦除原来的位置
                        DrawBoard(mvLastFrom, g);
                        DrawSelection(mvLastFrom, g);
                        //移动到新位置
                        DrawSelection(mvLastTo, g);
                        DrawPiece(mvLastTo, pcSelected, g);
                        bSelected = false;
                        int pcCaptured = pos.pcSquares[sqTo];
                        pos.MakeMove(sqFrom, sqTo);

                        iMOVE step;
                        step.from = sqFrom;
                        step.to = sqTo;
                        step.comment = textBoxComment.Text;
                        iMoveList.Add(step);

                        FENStep++;
                        string label = step.ToString();
                        if (FENStep % 2 == 1)
                            label = ((FENStep / 2 + 1).ToString() + "." + label);
                        label = label.PadLeft(8);
                        listboxMove.Items.Add(label);
                        if (piece > 0)
                            PlaySound("CAPTURE");
                        else
                            PlaySound("MOVE");

                        if (POSITION.PIECE_INDEX(pcCaptured) == 1 || pos.IsMate())
                        {//直接吃王或者绝杀
                            MessageBox.Show("祝贺你取得胜利！");
                            PlaySound("WIN");
                            App_inGame = false;
                        }
                    }
                    //else
                    //    PlaySound("ILLEGAL");
                }
            }
            else if (POSITION.SIDE(piece) == pos.sdPlayer)
            {
                ptSelected = new Point(x, y);
                pcSelected = cnPieceImages[piece];
                DrawSelection(ptSelected, g);
                bSelected = true;
            }

        }

        private void panelBoard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 10; y++)
                {
                    int piece = cnPieceImages[pos.iGetPiece(x, y)];
                    if (piece > 0)
                    {
                        Image image = ilCPieces.Images[piece];
                        if (bFlipped)
                            DrawPiece(new Point(8 - x, 9 - y), piece, g);
                        else
                            DrawPiece(new Point(x, y), piece, g);
                    }
                }
            if (FENStep > 0)
            {
                DrawSelection(mvLastFrom, g);
                DrawSelection(mvLastTo, g);
            }
        }

        public void DrawBoard(Point pt, Graphics g)
        {
            float xx = pt.X * gridSize;
            float yy = pt.Y * gridSize;
            RectangleF srcRect = new RectangleF(xx, yy, gridSize, gridSize);
            g.DrawImage(panelBoard.BackgroundImage, xx, yy, srcRect, GraphicsUnit.Pixel);
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            PgnFileStruct PGN;
            if (openPGNDialog.ShowDialog() == DialogResult.OK)
            {
                PGN = pos.ReadPgnFile(openPGNDialog.FileName);
                iMoveList = PGN.iMoveList;
            }
            else
                return;
            labelEvent.Text = PGN.Event;
            string result;
            switch (PGN.Result)
            {
                case "1-0":
                    result = "胜";
                    break;
                case "0-1":
                    result = "负";
                    break;
                case "1/2-1/2":
                    result = "和";
                    break;
                default:
                    result = "*";
                    break;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(PGN.RedTeam);
            sb.Append(' ');
            sb.Append(PGN.Red);
            sb.Append(" (先");
            sb.Append(result);
            sb.Append(") ");
            sb.Append(PGN.BlackTeam);
            sb.Append(' ');
            sb.Append(PGN.Black);
            labelPlayer.Text = sb.ToString();
            labelDateSite.Text = PGN.Date + " 弈于 " + PGN.Site;


            listboxMove.Items.Clear();

            if (iMoveList[0].comment == null)
                listboxMove.Items.Add("==开始==");
            else
                listboxMove.Items.Add("==开始==*");

            for (FENStep = 1; FENStep < iMoveList.Count; FENStep++)
            {
                iMOVE step = iMoveList[FENStep];
                string label = step.ToString();
                if (FENStep % 2 == 1)
                    label = ((FENStep / 2 + 1).ToString() + "." + label);
                label = label.PadLeft(8);
                if (step.comment != null)
                    label += "*";
                listboxMove.Items.Add(label);
            }
            pos.FromFEN(PGN.StartFEN);
            FENStep = 0;
            listboxMove.SelectedIndex = 0;
            panelBoard.Refresh();
            App_inGame = false;
        }

        private void menuFlipBoard_Click(object sender, EventArgs e)
        {
            menuFlipBoard.Checked = !menuFlipBoard.Checked;
            bFlipped = menuFlipBoard.Checked;
            panelBoard.Refresh();
        }

        private void menuCopyFEN_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(pos.ToFen());
        }

        private void menuPasteFEN_Click(object sender, EventArgs e)
        {
            try
            {
                pos.FromFEN(Clipboard.GetText());
            }
            catch (Exception)
            {
                MessageBox.Show("不能识别的局面");
            }
            NewFEN();
        }

        private void menuLoadFEN_Click(object sender, EventArgs e)
        {
            if (openFENDialog.ShowDialog() == DialogResult.OK)
            {
                var fileStream = openFENDialog.OpenFile();
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string fen = reader.ReadToEnd();
                    pos.FromFEN(fen);
                    NewFEN();
                }
            }
        }

        private void menuSaveFEN_Click(object sender, EventArgs e)
        {
            if (saveFENDialog.ShowDialog() == DialogResult.OK)
            {
                var fileStream = saveFENDialog.OpenFile();
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    writer.WriteLine(pos.ToFen());
                }
            }
        }

        private void menuAIRed_Click(object sender, EventArgs e)
        {
            menuAIRed.Checked = !menuAIRed.Checked;
            menuPonder.Checked = false;
            bFlipped = true;
        }

        private void menuAIBlack_Click(object sender, EventArgs e)
        {
            menuAIBlack.Checked = !menuAIBlack.Checked;
            menuPonder.Checked = false;
            bFlipped = false;
        }

        private void menuPonder_Click(object sender, EventArgs e)
        {
            menuAIRed.Checked = false;
            menuAIBlack.Checked = false;
            menuPonder.Checked = true;
        }

        private void menuAboutEngine_Click(object sender, EventArgs e)
        {
            MessageBox.Show("引擎：鼹鼠象棋\n版本：0.1\n作者：荣宇明\n用户：测试人员", "UCCI引擎");
        }

        private void listboxMove_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listboxMove.SelectedIndex < 0)
                return;
            textBoxComment.Text = iMoveList[listboxMove.SelectedIndex].comment;
            if (listboxMove.SelectedIndex > FENStep)
            {
                for (int i = FENStep + 1; i <= listboxMove.SelectedIndex; i++)
                    pos.MakeMove(iMoveList[i].from, iMoveList[i].to);
            }
            else
            {
                for (int i = FENStep - 1; i >= listboxMove.SelectedIndex; i--)
                    pos.UnmakeMove();
            }
            FENStep = listboxMove.SelectedIndex;
            mvLastFrom = POSITION.iCoord2XY(iMoveList[listboxMove.SelectedIndex].from, bFlipped);
            mvLastTo = POSITION.iCoord2XY(iMoveList[listboxMove.SelectedIndex].to, bFlipped);
            panelBoard.Refresh();
        }

        private void menuActivePositionTest_Click(object sender, EventArgs e)
        {
            string sourceDirectory = @"J:\象棋\全局\1-23届五羊杯";
            IEnumerable<string> pgnFiles = Directory.EnumerateFiles(sourceDirectory, "*.PGN", SearchOption.AllDirectories);
            int nFile = 0;
            //统计棋子活动位置的数组
            int[,] activeGrid = new int[2, 256];

            foreach (string fileName in pgnFiles)
            {
                Console.WriteLine(fileName.Substring(sourceDirectory.Length + 1));
                PgnFileStruct pgn = pos.ReadPgnFile(fileName);
                nFile++;
                pos.FromFEN(pgn.StartFEN);
                int side = 0;
                for (int i = 1; i < pgn.iMoveList.Count; i++)
                {
                    iMOVE step = pgn.iMoveList[i];
                    if (pos.pcSquares[step.to] > 0)
                        activeGrid[side, step.to]++;
                    pos.MakeMove(step.from, step.to);
                    side = 1 ^ side;
                }
            }
            POSITION.Write2Csv(@"J:\xqtest\capture.csv", activeGrid);
            MessageBox.Show(String.Format("Finish reading.Total {0} files", nFile));
        }

        private void menuEvaluate_Click(object sender, EventArgs e)
        {
            SEARCH engine = new SEARCH(pos);
            engine.SearchQuiesce(-5000, 5000);
            //WriteMap2Csv(pos.attackMap, @"J:\xqtest\attack.csv");
            //WriteMap2Csv(pos.connectivityMap, @"J:\xqtest\connectivity.csv");
        }

        private void menuContinuousEval_Click(object sender, EventArgs e)
        {
            string fileName = @"J:\象棋\全局\1-23届五羊杯\第01届五羊杯象棋赛(1981)\第01局-胡荣华(红先负)柳大华.PGN";
            PgnFileStruct pgn = pos.ReadPgnFile(fileName);
            
            pos.FromFEN(pgn.StartFEN);
            SEARCH engine = new SEARCH(pos);
            engine.SearchQuiesce(-5000, 5000);
            for (int i = 1; i < pgn.iMoveList.Count; i++)
            {
                iMOVE step = pgn.iMoveList[i];
                engine.board.MakeMove(step.from, step.to);
                int score = engine.SearchQuiesce(-5000, 5000);
                if (i % 2 == 1)
                    Console.Write("{0}. {1}  ", (i + 1) / 2, score);
                else
                    Console.WriteLine(score);
            }

            //Write2Csv(@"J:\xqtest\eval.csv", pos.ivpc, totalMoves, 48);
            /* 用顶级人类选手的对局来测试评估审局函数的有效性。
             * 理想情况下，双方分数应呈锯齿状交替上升，除去吃子的步骤，应该稳定渐变。
             */
        }

        private void menuBatchEval_Click(object sender, EventArgs e)
        {
            pos.TestEval();
        }

        public void DrawSelection(Point pt, Graphics g)
        {
            g.DrawImage(ilCPieces.Images[0], pt.X * gridSize, pt.Y * gridSize);
        }

        public void DrawPiece(Point pt, int pc, Graphics g)
        {
            g.DrawImage(ilCPieces.Images[pc], pt.X * gridSize, pt.Y * gridSize);
        }

        void PlaySound(string szWavFile)
        {
            if (!App_bSound)
                return;
            try
            {
                soundPlayer.SoundLocation = App_szPath + "SOUNDS\\" + szWavFile + ".WAV";
                soundPlayer.Load();
                soundPlayer.Play();
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot find sound file");
            }
        }

        void Write2Csv(string csvPath, int[] array)
        {
            using (FileStream fs = new FileStream(csvPath.Trim(), FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                {
                    sw.AutoFlush = false;
                    foreach (int i in array)
                    {
                        sw.WriteLine(i);
                    }
                    sw.Flush();
                }
            }
        }

        void Write2Csv(string csvPath, int[,] array, int nrow, int ncol)
        {
            using (FileStream fs = new FileStream(csvPath.Trim(), FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                {
                    sw.AutoFlush = false;
                    sw.Write("Step, Total, Total R, Total B, MAT R, MAT B, POS R, POS B, Conn R, Conn B, Pair R, Pair B,Tactic R,Tactic B,,,");
                    sw.WriteLine("帅,车,车,炮,炮,马,马,兵,兵,兵,兵,兵,相,相,仕,仕,将,车,车,炮,炮,马,马,卒,卒,卒,卒,卒,象,象,士,士");
                    for (int row = 0; row < nrow; row++)
                    {
                        for (int col = 0; col < ncol; col++)
                        {
                            sw.Write("{0},", array[row, col]);
                        }
                        sw.WriteLine();
                    }
                    sw.Flush();
                }
            }
        }

        void WriteMap2Csv(int[,] map, string csvPath)
        {
            using (FileStream fs = new FileStream(csvPath.Trim(), FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                {
                    sw.AutoFlush = false;
                    for (int y = 0; y < 10; y++)
                    {
                        for (int x = 0; x < 9; x++)
                        {
                            int sq = POSITION.iXY2Coord(x, y);
                            sw.Write("{0} | {1},", map[0, sq], map[1, sq]);
                        }
                        sw.WriteLine();
                    }
                    sw.Flush();
                }
            }
        }

    }
}
