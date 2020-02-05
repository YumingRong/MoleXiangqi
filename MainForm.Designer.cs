namespace MoleXiangqi
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.saveFENDialog = new System.Windows.Forms.SaveFileDialog();
            this.MenuAboutEngine = new System.Windows.Forms.ToolStripMenuItem();
            this.帮助HToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuPonder = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuAIBlack = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuAIRed = new System.Windows.Forms.ToolStripMenuItem();
            this.电脑ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuSaveFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuLoadFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuPasteFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuCopyFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuEditFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuFlipBoard = new System.Windows.Forms.ToolStripMenuItem();
            this.局面ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.连续审局ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewGameMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MenuBatchEvaluation = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuActivePositionTest = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuEvaluate = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuContinuousEval = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuBatchEval = new System.Windows.Forms.ToolStripMenuItem();
            this.ilCPieces = new System.Windows.Forms.ImageList(this.components);
            this.openFENDialog = new System.Windows.Forms.OpenFileDialog();
            this.ListboxMove = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.PanelBoard = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxComment = new System.Windows.Forms.TextBox();
            this.labelDateSite = new System.Windows.Forms.Label();
            this.labelPlayer = new System.Windows.Forms.Label();
            this.labelEvent = new System.Windows.Forms.Label();
            this.openPGNDialog = new System.Windows.Forms.OpenFileDialog();
            this.MenuRuleTest = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStrip1.SuspendLayout();
            this.PanelBoard.SuspendLayout();
            this.SuspendLayout();
            // 
            // saveFENDialog
            // 
            this.saveFENDialog.Filter = "象棋局面文件(*.FEN)|*.FEN";
            this.saveFENDialog.Title = "导出到局面文件";
            // 
            // MenuAboutEngine
            // 
            this.MenuAboutEngine.Name = "MenuAboutEngine";
            this.MenuAboutEngine.Size = new System.Drawing.Size(153, 22);
            this.MenuAboutEngine.Text = "关于UCCI引擎";
            this.MenuAboutEngine.Click += new System.EventHandler(this.MenuAboutEngine_Click);
            // 
            // 帮助HToolStripMenuItem
            // 
            this.帮助HToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuAboutEngine});
            this.帮助HToolStripMenuItem.Name = "帮助HToolStripMenuItem";
            this.帮助HToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.帮助HToolStripMenuItem.Text = "帮助(&H)";
            // 
            // MenuPonder
            // 
            this.MenuPonder.Name = "MenuPonder";
            this.MenuPonder.Size = new System.Drawing.Size(141, 22);
            this.MenuPonder.Text = "后台思考(&P)";
            this.MenuPonder.Click += new System.EventHandler(this.MenuPonder_Click);
            // 
            // MenuAIBlack
            // 
            this.MenuAIBlack.Name = "MenuAIBlack";
            this.MenuAIBlack.Size = new System.Drawing.Size(141, 22);
            this.MenuAIBlack.Text = "电脑执黑(&B)";
            this.MenuAIBlack.Click += new System.EventHandler(this.MenuAIBlack_Click);
            // 
            // MenuAIRed
            // 
            this.MenuAIRed.Name = "MenuAIRed";
            this.MenuAIRed.Size = new System.Drawing.Size(141, 22);
            this.MenuAIRed.Text = "电脑执红(&R)";
            this.MenuAIRed.Click += new System.EventHandler(this.MenuAIRed_Click);
            // 
            // 电脑ToolStripMenuItem
            // 
            this.电脑ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuAIRed,
            this.MenuAIBlack,
            this.MenuPonder});
            this.电脑ToolStripMenuItem.Name = "电脑ToolStripMenuItem";
            this.电脑ToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.电脑ToolStripMenuItem.Text = "电脑(&E)";
            // 
            // MenuSaveFEN
            // 
            this.MenuSaveFEN.Name = "MenuSaveFEN";
            this.MenuSaveFEN.Size = new System.Drawing.Size(235, 22);
            this.MenuSaveFEN.Text = "导出到局面文件(&S)...";
            this.MenuSaveFEN.Click += new System.EventHandler(this.MenuSaveFEN_Click);
            // 
            // MenuLoadFEN
            // 
            this.MenuLoadFEN.Name = "MenuLoadFEN";
            this.MenuLoadFEN.Size = new System.Drawing.Size(235, 22);
            this.MenuLoadFEN.Text = "从局面文件导入(&L)...";
            this.MenuLoadFEN.Click += new System.EventHandler(this.MenuLoadFEN_Click);
            // 
            // MenuPasteFEN
            // 
            this.MenuPasteFEN.Name = "MenuPasteFEN";
            this.MenuPasteFEN.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Insert)));
            this.MenuPasteFEN.Size = new System.Drawing.Size(235, 22);
            this.MenuPasteFEN.Text = "粘贴局面代码(&P)";
            this.MenuPasteFEN.Click += new System.EventHandler(this.MenuPasteFEN_Click);
            // 
            // MenuCopyFEN
            // 
            this.MenuCopyFEN.Name = "MenuCopyFEN";
            this.MenuCopyFEN.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Insert)));
            this.MenuCopyFEN.Size = new System.Drawing.Size(235, 22);
            this.MenuCopyFEN.Text = "复制局面代码(&C)";
            this.MenuCopyFEN.Click += new System.EventHandler(this.MenuCopyFEN_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(232, 6);
            // 
            // MenuEditFEN
            // 
            this.MenuEditFEN.Name = "MenuEditFEN";
            this.MenuEditFEN.Size = new System.Drawing.Size(235, 22);
            this.MenuEditFEN.Text = "编辑局面(&E) Ctrl+E...";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(232, 6);
            // 
            // MenuFlipBoard
            // 
            this.MenuFlipBoard.Name = "MenuFlipBoard";
            this.MenuFlipBoard.Size = new System.Drawing.Size(235, 22);
            this.MenuFlipBoard.Text = "翻转棋盘(&F)";
            this.MenuFlipBoard.Click += new System.EventHandler(this.MenuFlipBoard_Click);
            // 
            // 局面ToolStripMenuItem
            // 
            this.局面ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuFlipBoard,
            this.toolStripMenuItem1,
            this.连续审局ToolStripMenuItem,
            this.MenuEditFEN,
            this.toolStripMenuItem2,
            this.MenuCopyFEN,
            this.MenuPasteFEN,
            this.MenuLoadFEN,
            this.MenuSaveFEN});
            this.局面ToolStripMenuItem.Name = "局面ToolStripMenuItem";
            this.局面ToolStripMenuItem.Size = new System.Drawing.Size(60, 20);
            this.局面ToolStripMenuItem.Text = "局面(&P)";
            // 
            // 连续审局ToolStripMenuItem
            // 
            this.连续审局ToolStripMenuItem.Name = "连续审局ToolStripMenuItem";
            this.连续审局ToolStripMenuItem.Size = new System.Drawing.Size(235, 22);
            this.连续审局ToolStripMenuItem.Text = "连续审局";
            // 
            // NewGameMenu
            // 
            this.NewGameMenu.Name = "NewGameMenu";
            this.NewGameMenu.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.NewGameMenu.Size = new System.Drawing.Size(186, 22);
            this.NewGameMenu.Text = "新的对局(&N)";
            this.NewGameMenu.Click += new System.EventHandler(this.NewGameMenu_Click);
            // 
            // 文件ToolStripMenuItem
            // 
            this.文件ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewGameMenu,
            this.MenuOpen});
            this.文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            this.文件ToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.文件ToolStripMenuItem.Text = "文件(&F)";
            // 
            // MenuOpen
            // 
            this.MenuOpen.Name = "MenuOpen";
            this.MenuOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.MenuOpen.Size = new System.Drawing.Size(186, 22);
            this.MenuOpen.Text = "打开(&O)...";
            this.MenuOpen.Click += new System.EventHandler(this.MenuOpen_Click);
            // 
            // MenuStrip1
            // 
            this.MenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.文件ToolStripMenuItem,
            this.局面ToolStripMenuItem,
            this.电脑ToolStripMenuItem,
            this.帮助HToolStripMenuItem,
            this.MenuBatchEvaluation});
            this.MenuStrip1.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip1.Name = "MenuStrip1";
            this.MenuStrip1.Size = new System.Drawing.Size(748, 24);
            this.MenuStrip1.TabIndex = 2;
            this.MenuStrip1.Text = "MenuStrip1";
            // 
            // MenuBatchEvaluation
            // 
            this.MenuBatchEvaluation.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuActivePositionTest,
            this.MenuEvaluate,
            this.MenuContinuousEval,
            this.MenuBatchEval,
            this.MenuRuleTest});
            this.MenuBatchEvaluation.Name = "MenuBatchEvaluation";
            this.MenuBatchEvaluation.Size = new System.Drawing.Size(45, 20);
            this.MenuBatchEvaluation.Text = "测试";
            // 
            // MenuActivePositionTest
            // 
            this.MenuActivePositionTest.Name = "MenuActivePositionTest";
            this.MenuActivePositionTest.Size = new System.Drawing.Size(180, 22);
            this.MenuActivePositionTest.Text = "棋子活动范围";
            this.MenuActivePositionTest.Click += new System.EventHandler(this.MenuActivePositionTest_Click);
            // 
            // MenuEvaluate
            // 
            this.MenuEvaluate.Name = "MenuEvaluate";
            this.MenuEvaluate.Size = new System.Drawing.Size(180, 22);
            this.MenuEvaluate.Text = "审局";
            this.MenuEvaluate.Click += new System.EventHandler(this.MenuEvaluate_Click);
            // 
            // MenuContinuousEval
            // 
            this.MenuContinuousEval.Name = "MenuContinuousEval";
            this.MenuContinuousEval.Size = new System.Drawing.Size(180, 22);
            this.MenuContinuousEval.Text = "连续审局";
            this.MenuContinuousEval.Click += new System.EventHandler(this.MenuContinuousEval_Click);
            // 
            // MenuBatchEval
            // 
            this.MenuBatchEval.Name = "MenuBatchEval";
            this.MenuBatchEval.Size = new System.Drawing.Size(180, 22);
            this.MenuBatchEval.Text = "批量连续审局";
            this.MenuBatchEval.Click += new System.EventHandler(this.MenuBatchEval_Click);
            // 
            // ilCPieces
            // 
            this.ilCPieces.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilCPieces.ImageStream")));
            this.ilCPieces.TransparentColor = System.Drawing.Color.Transparent;
            this.ilCPieces.Images.SetKeyName(0, "oos.gif");
            this.ilCPieces.Images.SetKeyName(1, "rk.gif");
            this.ilCPieces.Images.SetKeyName(2, "rr.gif");
            this.ilCPieces.Images.SetKeyName(3, "rc.gif");
            this.ilCPieces.Images.SetKeyName(4, "rn.gif");
            this.ilCPieces.Images.SetKeyName(5, "rp.gif");
            this.ilCPieces.Images.SetKeyName(6, "rb.gif");
            this.ilCPieces.Images.SetKeyName(7, "ra.gif");
            this.ilCPieces.Images.SetKeyName(8, "bk.gif");
            this.ilCPieces.Images.SetKeyName(9, "br.gif");
            this.ilCPieces.Images.SetKeyName(10, "bc.gif");
            this.ilCPieces.Images.SetKeyName(11, "bn.gif");
            this.ilCPieces.Images.SetKeyName(12, "bp.gif");
            this.ilCPieces.Images.SetKeyName(13, "bb.gif");
            this.ilCPieces.Images.SetKeyName(14, "ba.gif");
            // 
            // openFENDialog
            // 
            this.openFENDialog.Filter = "象棋局面文件(*.FEN)|*.FEN";
            this.openFENDialog.Title = "从局面文件导入";
            // 
            // ListboxMove
            // 
            this.ListboxMove.FormattingEnabled = true;
            this.ListboxMove.ItemHeight = 12;
            this.ListboxMove.Items.AddRange(new object[] {
            "==开始=="});
            this.ListboxMove.Location = new System.Drawing.Point(633, 119);
            this.ListboxMove.Name = "ListboxMove";
            this.ListboxMove.Size = new System.Drawing.Size(103, 292);
            this.ListboxMove.TabIndex = 0;
            this.ListboxMove.SelectedIndexChanged += new System.EventHandler(this.ListboxMove_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(631, 104);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "着法列表";
            // 
            // PanelBoard
            // 
            this.PanelBoard.AutoSize = true;
            this.PanelBoard.BackColor = System.Drawing.Color.LightGray;
            this.PanelBoard.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("PanelBoard.BackgroundImage")));
            this.PanelBoard.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.PanelBoard.Controls.Add(this.label2);
            this.PanelBoard.Controls.Add(this.textBoxComment);
            this.PanelBoard.Controls.Add(this.labelDateSite);
            this.PanelBoard.Controls.Add(this.labelPlayer);
            this.PanelBoard.Controls.Add(this.labelEvent);
            this.PanelBoard.Controls.Add(this.label1);
            this.PanelBoard.Controls.Add(this.ListboxMove);
            this.PanelBoard.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.PanelBoard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PanelBoard.Location = new System.Drawing.Point(0, 24);
            this.PanelBoard.Name = "PanelBoard";
            this.PanelBoard.Size = new System.Drawing.Size(748, 580);
            this.PanelBoard.TabIndex = 3;
            this.PanelBoard.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelBoard_Paint);
            this.PanelBoard.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PanelBoard_MouseClick);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(549, 415);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "注释";
            // 
            // textBoxComment
            // 
            this.textBoxComment.Location = new System.Drawing.Point(539, 430);
            this.textBoxComment.Multiline = true;
            this.textBoxComment.Name = "textBoxComment";
            this.textBoxComment.Size = new System.Drawing.Size(197, 138);
            this.textBoxComment.TabIndex = 5;
            // 
            // labelDateSite
            // 
            this.labelDateSite.AutoSize = true;
            this.labelDateSite.Location = new System.Drawing.Point(534, 60);
            this.labelDateSite.Name = "labelDateSite";
            this.labelDateSite.Size = new System.Drawing.Size(71, 12);
            this.labelDateSite.TabIndex = 4;
            this.labelDateSite.Text = "Date && Site";
            // 
            // labelPlayer
            // 
            this.labelPlayer.AutoSize = true;
            this.labelPlayer.Location = new System.Drawing.Point(534, 39);
            this.labelPlayer.Name = "labelPlayer";
            this.labelPlayer.Size = new System.Drawing.Size(23, 12);
            this.labelPlayer.TabIndex = 3;
            this.labelPlayer.Text = "VS.";
            // 
            // labelEvent
            // 
            this.labelEvent.AutoSize = true;
            this.labelEvent.Location = new System.Drawing.Point(534, 18);
            this.labelEvent.Name = "labelEvent";
            this.labelEvent.Size = new System.Drawing.Size(35, 12);
            this.labelEvent.TabIndex = 2;
            this.labelEvent.Text = "Event";
            // 
            // openPGNDialog
            // 
            this.openPGNDialog.Filter = "象棋对局面文件(*.PGN)|*.PGN";
            this.openPGNDialog.InitialDirectory = "J:\\象棋\\全局\\1-23届五羊杯\\第01届五羊杯象棋赛(1981)";
            // 
            // MenuRuleTest
            // 
            this.MenuRuleTest.Name = "MenuRuleTest";
            this.MenuRuleTest.Size = new System.Drawing.Size(180, 22);
            this.MenuRuleTest.Text = "棋规";
            this.MenuRuleTest.Click += new System.EventHandler(this.MenuRuleTest_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(748, 604);
            this.Controls.Add(this.PanelBoard);
            this.Controls.Add(this.MenuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MenuStrip1;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "鼹鼠象棋";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.MenuStrip1.ResumeLayout(false);
            this.MenuStrip1.PerformLayout();
            this.PanelBoard.ResumeLayout(false);
            this.PanelBoard.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SaveFileDialog saveFENDialog;
        private System.Windows.Forms.ToolStripMenuItem MenuAboutEngine;
        private System.Windows.Forms.ToolStripMenuItem 帮助HToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MenuPonder;
        private System.Windows.Forms.ToolStripMenuItem MenuAIBlack;
        private System.Windows.Forms.ToolStripMenuItem MenuAIRed;
        private System.Windows.Forms.ToolStripMenuItem 电脑ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MenuSaveFEN;
        private System.Windows.Forms.ToolStripMenuItem MenuLoadFEN;
        private System.Windows.Forms.ToolStripMenuItem MenuPasteFEN;
        private System.Windows.Forms.ToolStripMenuItem MenuCopyFEN;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem MenuEditFEN;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem MenuFlipBoard;
        private System.Windows.Forms.ToolStripMenuItem 局面ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NewGameMenu;
        private System.Windows.Forms.ToolStripMenuItem 文件ToolStripMenuItem;
        private System.Windows.Forms.MenuStrip MenuStrip1;
        private System.Windows.Forms.ImageList ilCPieces;
        private System.Windows.Forms.OpenFileDialog openFENDialog;
        private System.Windows.Forms.ListBox ListboxMove;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel PanelBoard;
        private System.Windows.Forms.ToolStripMenuItem MenuOpen;
        private System.Windows.Forms.OpenFileDialog openPGNDialog;
        private System.Windows.Forms.Label labelEvent;
        private System.Windows.Forms.Label labelPlayer;
        private System.Windows.Forms.Label labelDateSite;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxComment;
        private System.Windows.Forms.ToolStripMenuItem MenuBatchEvaluation;
        private System.Windows.Forms.ToolStripMenuItem MenuActivePositionTest;
        private System.Windows.Forms.ToolStripMenuItem MenuEvaluate;
        private System.Windows.Forms.ToolStripMenuItem 连续审局ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MenuContinuousEval;
        private System.Windows.Forms.ToolStripMenuItem MenuBatchEval;
        private System.Windows.Forms.ToolStripMenuItem MenuRuleTest;
    }
}