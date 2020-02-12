using System;
using System.Collections.Generic;
using System.IO;
using MathNet.Numerics.Statistics;
using System.Drawing;
using System.Media;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace MoleXiangqi
{
    public partial class MainForm : Form
    {
        bool App_bSound = true;
        readonly string App_szPath;
        bool App_inGame;
        Point ptLastFrom, ptLastTo, ptSelected;
        int pcSelected, pcLast;
        int FENStep; //用来决定要不要显示上一步方框
        bool bFlipped = false;
        bool bSelected;
        List<MOVE> MoveList;
        List<string> CommentList;
        POSITION pos;
        POSITION engine;
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
            MoveList = new List<MOVE>();
            CommentList = new List<string>();
            soundPlayer = new SoundPlayer();
            string path = Application.StartupPath;
            path = Path.GetDirectoryName(path);
            path = Path.GetDirectoryName(path);
            App_szPath = Path.Combine(path, "Resources");
            engine = new POSITION();
        }

        private void NewGameMenu_Click(object sender, EventArgs e)
        {
            pos.FromFEN(POSITION.cszStartFen);
            engine.FromFEN(POSITION.cszStartFen);
            NewGameAsync();
        }

        //相当于给引擎发出option newgame 指令
        private async void NewGameAsync()
        {
            //swap side
            if (MenuAIBlack.Checked)
            {
                MenuAIRed.Checked = true;
                MenuAIBlack.Checked = false;
                bFlipped = false;
            }
            else if (MenuAIRed.Checked)
            {
                MenuAIRed.Checked = false;
                MenuAIBlack.Checked = true;
                bFlipped = true;
            }
            FENStep = 0;
            bSelected = false;
            ListboxMove.Items.Clear();
            ListboxMove.Items.Add("==开始==");
            MoveList.Clear();
            MoveList.Add(new MOVE());
            CommentList.Add("");
            PanelBoard.Refresh();
            App_inGame = true;

            await GoAsync();
        }

        async Task GoAsync()
        {
            //相当于go depth指令和bestmove反馈
            while (pos.sdPlayer == 1 && MenuAIBlack.Checked || pos.sdPlayer == 0 && MenuAIRed.Checked)
            {
                Task<MOVE> GetBestMove = Task<MOVE>.Run(() => engine.SearchMain(1));
                MOVE bestmove = await GetBestMove;
                MakeMove(bestmove.sqSrc, bestmove.sqDst);
            }

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //position startpos指令
            pos.FromFEN(POSITION.cszStartFen);
            engine.FromFEN(POSITION.cszStartFen);
            NewGameAsync();
        }

        private async void PanelBoard_MouseClick(object sender, MouseEventArgs e)
        {
            if (!App_inGame)
                return;
            int x = e.X / gridSize;
            int y = e.Y / gridSize;

            if (x < 0 || x > 9 || y < 0 || y > 10)
                return;
            int piece;
            if (bFlipped)
                piece = pos.UI_GetFlippedPiece(x, y);
            else
                piece = pos.UI_GetPiece(x, y);

            Graphics g = PanelBoard.CreateGraphics();
            if (bSelected)
            {
                if (POSITION.SIDE(piece) == pos.sdPlayer && ptSelected != new Point(x, y))
                {
                    //又选择别的己方子
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
                        sqFrom = POSITION.UI_XY2Coord(8 - ptSelected.X, 9 - ptSelected.Y);
                        sqTo = POSITION.UI_XY2Coord(8 - x, 9 - y);
                    }
                    else
                    {
                        sqFrom = POSITION.UI_XY2Coord(ptSelected.X, ptSelected.Y);
                        sqTo = POSITION.UI_XY2Coord(x, y);
                    }
                    MakeMove(sqFrom, sqTo);
                    await GoAsync();
                }
            }
            else if (POSITION.SIDE(piece) == pos.sdPlayer)
                if (pos.sdPlayer == 0 && !MenuAIRed.Checked || pos.sdPlayer == 1 && !MenuAIBlack.Checked)
                {
                    ptSelected = new Point(x, y);
                    pcSelected = cnPieceImages[piece];
                    DrawSelection(ptSelected, g);
                    bSelected = true;
                }

        }

        private void PanelBoard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 10; y++)
                {
                    int piece = cnPieceImages[pos.UI_GetPiece(x, y)];
                    if (piece > 0)
                    {
                        if (bFlipped)
                            DrawPiece(new Point(8 - x, 9 - y), piece, g);
                        else
                            DrawPiece(new Point(x, y), piece, g);
                    }
                }
            if (FENStep > 0)
            {
                DrawSelection(ptLastFrom, g);
                DrawSelection(ptLastTo, g);
            }
        }

        public void DrawBoard(Point pt, Graphics g)
        {
            float xx = pt.X * gridSize;
            float yy = pt.Y * gridSize;
            RectangleF srcRect = new RectangleF(xx, yy, gridSize, gridSize);
            if (g != null)
                g.DrawImage(PanelBoard.BackgroundImage, xx, yy, srcRect, GraphicsUnit.Pixel);
        }

        private void MenuOpen_Click(object sender, EventArgs e)
        {
            PgnFileStruct PGN;
            if (openPGNDialog.ShowDialog() == DialogResult.OK)
            {
                PGN = pos.ReadPgnFile(openPGNDialog.FileName);
                MoveList = PGN.MoveList;
                CommentList = PGN.CommentList;
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


            ListboxMove.Items.Clear();

            if (string.IsNullOrEmpty(CommentList[0]))
                ListboxMove.Items.Add("==开始==");
            else
                ListboxMove.Items.Add("==开始==*");

            for (FENStep = 1; FENStep < MoveList.Count; FENStep++)
            {
                MOVE step = MoveList[FENStep];
                string label = step.ToString();
                if (FENStep % 2 == 1)
                    label = ((FENStep / 2 + 1).ToString() + "." + label);
                label = label.PadLeft(8);
                if (!string.IsNullOrEmpty(CommentList[FENStep]))
                    label += "*";
                ListboxMove.Items.Add(label);
            }
            pos.FromFEN(PGN.StartFEN);
            engine.FromFEN(PGN.StartFEN);
            FENStep = 0;
            ListboxMove.SelectedIndex = 0;
            PanelBoard.Refresh();
            App_inGame = false;
        }

        private void MenuFlipBoard_Click(object sender, EventArgs e)
        {
            MenuFlipBoard.Checked = !MenuFlipBoard.Checked;
            bFlipped = MenuFlipBoard.Checked;
            PanelBoard.BackgroundImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
            PanelBoard.Refresh();
        }

        private void MenuCopyFEN_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(pos.ToFen());
        }

        private void MenuPasteFEN_Click(object sender, EventArgs e)
        {
            try
            {
                pos.FromFEN(Clipboard.GetText());
                engine.FromFEN(Clipboard.GetText());
            }
            catch (Exception)
            {
                MessageBox.Show("不能识别的局面");
            }
            NewGameAsync();
        }

        private void MenuLoadFEN_Click(object sender, EventArgs e)
        {
            if (openFENDialog.ShowDialog() == DialogResult.OK)
            {
                var fileStream = openFENDialog.OpenFile();
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string fen = reader.ReadToEnd();
                    pos.FromFEN(fen);
                    engine.FromFEN(fen);
                    NewGameAsync();
                }
            }
        }

        private void MenuSaveFEN_Click(object sender, EventArgs e)
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

        private async void MenuAIRed_Click(object sender, EventArgs e)
        {
            MenuAIRed.Checked = !MenuAIRed.Checked;
            MenuPonder.Checked = false;
            //bFlipped = true;
            if (MenuAIRed.Checked && pos.sdPlayer == 0 || MenuAIBlack.Checked && pos.sdPlayer == 1)
                await GoAsync();
        }

        private void MenuAIBlack_Click(object sender, EventArgs e)
        {
            MenuAIBlack.Checked = !MenuAIBlack.Checked;
            MenuPonder.Checked = false;
            //bFlipped = false;
        }

        private void MenuPonder_Click(object sender, EventArgs e)
        {
            MenuAIRed.Checked = false;
            MenuAIBlack.Checked = false;
            MenuPonder.Checked = true;
        }

        //该函数相当于UCCI 的id命令
        private void MenuAboutEngine_Click(object sender, EventArgs e)
        {
            MessageBox.Show("引擎：鼹鼠象棋\n版本：0.1\n作者：荣宇明\n用户：测试人员", "UCCI引擎");
        }

        private void ListboxMove_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ListboxMove.SelectedIndex < 0)
                return;
            textBoxComment.Text = CommentList[ListboxMove.SelectedIndex];
            if (ListboxMove.SelectedIndex > FENStep)
            {
                for (int i = FENStep + 1; i <= ListboxMove.SelectedIndex; i++)
                    pos.MakeMove(MoveList[i]);
            }
            else
            {
                for (int i = FENStep - 1; i >= ListboxMove.SelectedIndex; i--)
                    pos.UnmakeMove();
            }
            FENStep = ListboxMove.SelectedIndex;
            ptLastFrom = POSITION.UI_Coord2XY(MoveList[ListboxMove.SelectedIndex].sqSrc, bFlipped);
            ptLastTo = POSITION.UI_Coord2XY(MoveList[ListboxMove.SelectedIndex].sqDst, bFlipped);
            PanelBoard.Refresh();
        }

        private void MenuActivePositionTest_Click(object sender, EventArgs e)
        {
            App_inGame = false;
            string sourceDirectory = @"G:\象棋\全局\1-23届五羊杯";
            IEnumerable<string> pgnFiles = Directory.EnumerateFiles(sourceDirectory, "*.PGN", SearchOption.AllDirectories);
            int nFile = 0;
            //统计棋子活动位置的数组
            int[,] activeGrid = new int[2, 256];

            foreach (string fileName in pgnFiles)
            {
                Console.WriteLine(fileName.Substring(sourceDirectory.Length + 1));
                PgnFileStruct pgn = engine.ReadPgnFile(fileName);
                nFile++;
                engine.FromFEN(pgn.StartFEN);
                int side = 0;
                for (int i = 1; i < pgn.MoveList.Count; i++)
                {
                    MOVE step = pgn.MoveList[i];
                    if (pos.pcSquares[step.sqDst] > 0)
                        activeGrid[side, step.sqDst]++;
                    engine.MakeMove(step);
                    side = 1 ^ side;
                }
            }
            POSITION.Write2Csv(@"G:\xqtest\capture.csv", activeGrid);
            MessageBox.Show(String.Format("Finish reading.Total {0} files", nFile));
        }

        private void MenuEvaluate_Click(object sender, EventArgs e)
        {
            //App_inGame = false;
            string fen = @"4kab2/4a4/4b4/9/9/5R3/9/4B1r2/4A4/1R1A1KBrc w - - 0 1";
            pos.FromFEN(fen);
            PanelBoard.Refresh();
            engine.FromFEN(fen);
            //engine.GenMoveTest();
            //NewGameAsync();
            //int score = engine.SearchQuiesce(-5000, 4998, 10);
            //MessageBox.Show("静态搜索分数" + score + ",搜索节点" + engine.stat.QuiesceNodes);
            //WriteMap2Csv(pos.attackMap, @"G:\xqtest\attack.csv");
            //WriteMap2Csv(pos.connectivityMap, @"G:\xqtest\connectivity.csv");
        }

        private void MenuContinuousEval_Click(object sender, EventArgs e)
        {
            App_inGame = false;
            string fileName = @"G:\象棋\全局\1-23届五羊杯\第01届五羊杯象棋赛(1981)\第01局-胡荣华(红先负)柳大华.PGN";
            PgnFileStruct pgn = pos.ReadPgnFile(fileName);

            pos.FromFEN(pgn.StartFEN);
            engine.FromFEN(pgn.StartFEN);
            engine.SearchQuiesce(-5000, 5000, 10);
            for (int i = 1; i < pgn.MoveList.Count; i++)
            {
                MOVE step = pgn.MoveList[i];
                engine.MakeMove(step);
                int score = -engine.SearchQuiesce(-5000, 5000, 10);
                if (i % 2 == 1)
                    Console.Write("{0}. {1}  ", (i + 1) / 2, score);
                else
                    Console.WriteLine(score);
            }

            //Write2Csv(@"G:\xqtest\eval.csv", pos.ivpc, totalMoves, 48);
            /* 用顶级人类选手的对局来测试评估审局函数的有效性。
             * 理想情况下，双方分数应呈锯齿状交替上升，除去吃子的步骤，应该稳定渐变。
             */
        }

        private void MenuBatchEval_Click(object sender, EventArgs e)
        {
            App_inGame = false;
            pos.TestEval();
        }

        public void DrawSelection(Point pt, Graphics g)
        {
            if (g != null)
                g.DrawImage(ilCPieces.Images[0], pt.X * gridSize, pt.Y * gridSize);
        }

        public void DrawPiece(Point pt, int pc, Graphics g)
        {
            if (g != null)
                g.DrawImage(ilCPieces.Images[pc], pt.X * gridSize, pt.Y * gridSize);
        }

        void PlaySound(string szWavFile)
        {
            if (!App_bSound)
                return;
            try
            {
                soundPlayer.SoundLocation = App_szPath + @"\SOUNDS\" + szWavFile + ".WAV";
                soundPlayer.Load();
                soundPlayer.Play();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Cannot find sound file");
            }
        }

        private void MenuRuleTest_Click(object sender, EventArgs e)
        {
            pos.RuleTest(@"G:\C#\MoleXiangqi\TestPGN\将帅或兵卒若每步都联合其他子长捉一子仍作负局.PGN");
        }

        void MakeMove(int sqFrom, int sqTo)
        {
            Graphics g = PanelBoard.CreateGraphics();
            if (pos.IsLegalMove(sqFrom, sqTo))
            {
                if (FENStep > 0)
                {
                    //擦除上一步的起始和结束位置选择框
                    DrawBoard(ptLastFrom, g);
                    DrawBoard(ptLastTo, g);
                    DrawPiece(ptLastTo, pcLast, g);
                }

                ptLastFrom = POSITION.UI_Coord2XY(sqFrom, bFlipped);
                ptLastTo = POSITION.UI_Coord2XY(sqTo, bFlipped);
                pcLast = cnPieceImages[pos.pcSquares[sqFrom]];

                //擦除原来的位置
                DrawBoard(ptLastFrom, g);
                DrawSelection(ptLastFrom, g);
                //移动到新位置
                DrawSelection(ptLastTo, g);
                DrawPiece(ptLastTo, pcLast, g);
                bSelected = false;
                int pcCaptured = pos.pcSquares[sqTo];

                MOVE step;
                step.sqSrc = sqFrom;
                step.sqDst = sqTo;
                step.pcSrc = pos.pcSquares[sqFrom];
                step.pcDst = pcCaptured;
                MoveList.Add(step);
                pos.MakeMove(step);
                engine.MakeMove(step);
                CommentList.Add(textBoxComment.Text);

                FENStep++;
                string label = step.ToString();
                if (FENStep % 2 == 1)
                    label = ((FENStep / 2 + 1).ToString() + "." + label);
                label = label.PadLeft(8);
                ListboxMove.Items.Add(label);
                if (pos.pcSquares[sqTo] > 0)
                    PlaySound("CAPTURE");
                else
                    PlaySound("MOVE");

                if (pos.IsMate() || POSITION.cnPieceKinds[pcCaptured] == 1)
                {//直接吃王或者绝杀
                    if (pos.sdPlayer == 1 && MenuAIBlack.Checked && !MenuAIRed.Checked
      || pos.sdPlayer == 0 && MenuAIRed.Checked && !MenuAIBlack.Checked)
                    {
                        PlaySound("WIN");
                    }
                    else if (pos.sdPlayer == 1 && !MenuAIBlack.Checked && MenuAIRed.Checked
                        || pos.sdPlayer == 0 && !MenuAIRed.Checked && MenuAIBlack.Checked)
                    {
                        PlaySound("LOSS");
                    }
                    if (pos.sdPlayer == 0)
                        MessageBox.Show("黑方胜！");
                    else
                        MessageBox.Show("红方胜！");
                    App_inGame = false;
                }
            }
            //else
            //    PlaySound("ILLEGAL");
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
                            int sq = POSITION.UI_XY2Coord(x, y);
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
