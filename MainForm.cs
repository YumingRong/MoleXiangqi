﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Drawing;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

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

        POSITION pos;
        const int gridSize = 57;
        SoundPlayer soundPlayer;
        readonly static int[] cnPieceImages = {
          0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
          1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 5, 6, 6, 7, 7,
          8,8,9,9,10,10,11,11,11,11,11,12,13,13,14,14
        };

        public MainForm()
        {
            InitializeComponent();
            pos = new POSITION();
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
            pos.iMoveList.Clear();
            pos.iMoveList.Add(new iMOVE());
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
                        pos.iMoveList.Add(step);

                        FENStep++;
                        string label = POSITION.iMove2Coord(sqFrom, sqTo);
                        if (FENStep % 2 == 1)
                            label = ((FENStep / 2 + 1).ToString() + "." + label);
                        label = label.PadLeft(8);
                        listboxMove.Items.Add(label);
                        if (piece > 0)
                            PlaySound("CAPTURE");
                        else
                            PlaySound("MOVE");

                        if (POSITION.PIECE_INDEX(pcCaptured) == 11 || pos.IsMate(pos.sdPlayer))
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
            string fileName = @"J:\全局\1-23届五羊杯\第01届五羊杯象棋赛(1981)\第01局-胡荣华(红先负)柳大华.PGN";
            pos.ReadPgnFile(fileName);

            //if (openPGNDialog.ShowDialog() == DialogResult.OK)
            //{
            //    pos.ReadPgnFile(openPGNDialog.FileName);
            //}
            labelEvent.Text = pos.PGN.Event;
            string result;
            switch (pos.PGN.Result)
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
            sb.Append(pos.PGN.RedTeam);
            sb.Append(' ');
            sb.Append(pos.PGN.Red);
            sb.Append(" (先");
            sb.Append(result);
            sb.Append(") ");
            sb.Append(pos.PGN.BlackTeam);
            sb.Append(' ');
            sb.Append(pos.PGN.Black);
            labelPlayer.Text = sb.ToString();
            labelDateSite.Text = pos.PGN.Date + " 弈于 " + pos.PGN.Site;


            listboxMove.Items.Clear();

            if (pos.iMoveList[0].comment == null)
                listboxMove.Items.Add("==开始==");
            else
                listboxMove.Items.Add("==开始==*");

            for (FENStep = 1; FENStep < pos.iMoveList.Count; FENStep++)
            {
                iMOVE step = pos.iMoveList[FENStep];
                string label = POSITION.iMove2Coord(step.from, step.to);
                if (FENStep % 2 == 1)
                    label = ((FENStep / 2 + 1).ToString() + "." + label);
                label = label.PadLeft(8);
                if (step.comment != null)
                    label += "*";
                listboxMove.Items.Add(label);
            }
            pos.FromFEN(pos.PGN.StartFEN);
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
            textBoxComment.Text = pos.iMoveList[listboxMove.SelectedIndex].comment;
            if (listboxMove.SelectedIndex > FENStep)
            {
                for (int i = FENStep + 1; i <= listboxMove.SelectedIndex; i++)
                    pos.MakeMove(pos.iMoveList[i].from, pos.iMoveList[i].to);
            }
            else
            {
                for (int i = FENStep - 1; i >= listboxMove.SelectedIndex; i--)
                    pos.UnmakeMove();
            }
            FENStep = listboxMove.SelectedIndex;
            mvLastFrom = POSITION.iCoord2XY(pos.iMoveList[listboxMove.SelectedIndex].from, bFlipped);
            mvLastTo = POSITION.iCoord2XY(pos.iMoveList[listboxMove.SelectedIndex].to, bFlipped);
            panelBoard.Refresh();
        }

        private void menuActivePositionTest_Click(object sender, EventArgs e)
        {
            string sourceDirectory = @"J:\全局\1-23届五羊杯";
            IEnumerable<string> pgnFiles = Directory.EnumerateFiles(sourceDirectory, "*.PGN", SearchOption.AllDirectories);
            int nFile = 0;
            pos.activeGrid = new int[2, 256];
            int[] gameLength = new int[500];

            foreach (string fileName in pgnFiles)
            {
                Console.WriteLine(fileName.Substring(sourceDirectory.Length + 1));
                if (!pos.ReadPgnFile(fileName))
                    Console.WriteLine("Fail to read!");
                nFile++;
                gameLength[pos.iMoveList.Count]++;
                //if (pos.iMoveList.Count < 30)
                //{
                //    Console.WriteLine(fileName.Substring(sourceDirectory.Length + 1) + "\t" + pos.iMoveList.Count);
                //    //File.Delete(fileName);
                //}
            }
            POSITION.Write2Csv(@"J:\xqtest\kingmove.csv", pos.activeGrid);
            Write2Csv(@"J:\xqtest\gamelength.csv", gameLength);
            MessageBox.Show(String.Format("Finish reading.Total {0} files", nFile));
        }

        private void menuEvaluate_Click(object sender, EventArgs e)
        {
            string fileName = @"J:\全局\1-23届五羊杯\第01届五羊杯象棋赛(1981)\第01局-胡荣华(红先负)柳大华.PGN";
            pos.ReadPgnFile(fileName);
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

    }
}
