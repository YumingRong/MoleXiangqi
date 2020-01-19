namespace XQWizardLight
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.ilCPieces = new System.Windows.Forms.ImageList(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewGameMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.局面ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFlipBoard = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuEditFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLoadFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSaveFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.电脑ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAIRed = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAIBlack = new System.Windows.Forms.ToolStripMenuItem();
            this.menuPonder = new System.Windows.Forms.ToolStripMenuItem();
            this.帮助HToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAboutEngine = new System.Windows.Forms.ToolStripMenuItem();
            this.panelBoard = new System.Windows.Forms.Panel();
            this.openFENDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFENDialog = new System.Windows.Forms.SaveFileDialog();
            this.menuCopyFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.menuPasteFEN = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.listMove = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.panelBoard.SuspendLayout();
            this.SuspendLayout();
            // 
            // ilCPieces
            // 
            this.ilCPieces.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilCPieces.ImageStream")));
            this.ilCPieces.TransparentColor = System.Drawing.Color.Transparent;
            this.ilCPieces.Images.SetKeyName(0, "oos.gif");
            this.ilCPieces.Images.SetKeyName(1, "rr.gif");
            this.ilCPieces.Images.SetKeyName(2, "rc.gif");
            this.ilCPieces.Images.SetKeyName(3, "rn.gif");
            this.ilCPieces.Images.SetKeyName(4, "rp.gif");
            this.ilCPieces.Images.SetKeyName(5, "rk.gif");
            this.ilCPieces.Images.SetKeyName(6, "rb.gif");
            this.ilCPieces.Images.SetKeyName(7, "ra.gif");
            this.ilCPieces.Images.SetKeyName(8, "br.gif");
            this.ilCPieces.Images.SetKeyName(9, "bc.gif");
            this.ilCPieces.Images.SetKeyName(10, "bn.gif");
            this.ilCPieces.Images.SetKeyName(11, "bp.gif");
            this.ilCPieces.Images.SetKeyName(12, "bk.gif");
            this.ilCPieces.Images.SetKeyName(13, "bb.gif");
            this.ilCPieces.Images.SetKeyName(14, "ba.gif");
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.文件ToolStripMenuItem,
            this.局面ToolStripMenuItem,
            this.电脑ToolStripMenuItem,
            this.帮助HToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(678, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 文件ToolStripMenuItem
            // 
            this.文件ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewGameMenu});
            this.文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            this.文件ToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.文件ToolStripMenuItem.Text = "文件(&F)";
            // 
            // NewGameMenu
            // 
            this.NewGameMenu.Name = "NewGameMenu";
            this.NewGameMenu.Size = new System.Drawing.Size(143, 22);
            this.NewGameMenu.Text = "新的对局(&N)";
            this.NewGameMenu.Click += new System.EventHandler(this.NewGameMenu_Click);
            // 
            // 局面ToolStripMenuItem
            // 
            this.局面ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFlipBoard,
            this.toolStripMenuItem1,
            this.menuEditFEN,
            this.toolStripMenuItem2,
            this.menuCopyFEN,
            this.menuPasteFEN,
            this.menuLoadFEN,
            this.menuSaveFEN});
            this.局面ToolStripMenuItem.Name = "局面ToolStripMenuItem";
            this.局面ToolStripMenuItem.Size = new System.Drawing.Size(60, 20);
            this.局面ToolStripMenuItem.Text = "局面(&P)";
            // 
            // menuFlipBoard
            // 
            this.menuFlipBoard.Name = "menuFlipBoard";
            this.menuFlipBoard.Size = new System.Drawing.Size(188, 22);
            this.menuFlipBoard.Text = "翻转棋盘(&F)";
            this.menuFlipBoard.Click += new System.EventHandler(this.menuFlipBoard_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(185, 6);
            // 
            // menuEditFEN
            // 
            this.menuEditFEN.Name = "menuEditFEN";
            this.menuEditFEN.Size = new System.Drawing.Size(188, 22);
            this.menuEditFEN.Text = "编辑局面(&E) Ctrl+E...";
            // 
            // menuLoadFEN
            // 
            this.menuLoadFEN.Name = "menuLoadFEN";
            this.menuLoadFEN.Size = new System.Drawing.Size(188, 22);
            this.menuLoadFEN.Text = "从局面文件导入(&L)...";
            this.menuLoadFEN.Click += new System.EventHandler(this.menuLoadFEN_Click);
            // 
            // menuSaveFEN
            // 
            this.menuSaveFEN.Name = "menuSaveFEN";
            this.menuSaveFEN.Size = new System.Drawing.Size(188, 22);
            this.menuSaveFEN.Text = "导出到局面文件(&S)...";
            this.menuSaveFEN.Click += new System.EventHandler(this.menuSaveFEN_Click);
            // 
            // 电脑ToolStripMenuItem
            // 
            this.电脑ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAIRed,
            this.menuAIBlack,
            this.menuPonder});
            this.电脑ToolStripMenuItem.Name = "电脑ToolStripMenuItem";
            this.电脑ToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.电脑ToolStripMenuItem.Text = "电脑(&E)";
            // 
            // menuAIRed
            // 
            this.menuAIRed.Name = "menuAIRed";
            this.menuAIRed.Size = new System.Drawing.Size(180, 22);
            this.menuAIRed.Text = "电脑执红(&R)";
            this.menuAIRed.Click += new System.EventHandler(this.menuAIRed_Click);
            // 
            // menuAIBlack
            // 
            this.menuAIBlack.Name = "menuAIBlack";
            this.menuAIBlack.Size = new System.Drawing.Size(180, 22);
            this.menuAIBlack.Text = "电脑执黑(&B)";
            this.menuAIBlack.Click += new System.EventHandler(this.menuAIBlack_Click);
            // 
            // menuPonder
            // 
            this.menuPonder.Name = "menuPonder";
            this.menuPonder.Size = new System.Drawing.Size(180, 22);
            this.menuPonder.Text = "后台思考(&P)";
            this.menuPonder.Click += new System.EventHandler(this.menuPonder_Click);
            // 
            // 帮助HToolStripMenuItem
            // 
            this.帮助HToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAboutEngine});
            this.帮助HToolStripMenuItem.Name = "帮助HToolStripMenuItem";
            this.帮助HToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.帮助HToolStripMenuItem.Text = "帮助(&H)";
            // 
            // menuAboutEngine
            // 
            this.menuAboutEngine.Name = "menuAboutEngine";
            this.menuAboutEngine.Size = new System.Drawing.Size(153, 22);
            this.menuAboutEngine.Text = "关于UCCI引擎";
            this.menuAboutEngine.Click += new System.EventHandler(this.menuAboutEngine_Click);
            // 
            // panelBoard
            // 
            this.panelBoard.AutoSize = true;
            this.panelBoard.BackColor = System.Drawing.Color.LightGray;
            this.panelBoard.BackgroundImage = global::XQWizardLight.Properties.Resources.qianhong;
            this.panelBoard.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.panelBoard.Controls.Add(this.label1);
            this.panelBoard.Controls.Add(this.listMove);
            this.panelBoard.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.panelBoard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBoard.Location = new System.Drawing.Point(0, 24);
            this.panelBoard.Name = "panelBoard";
            this.panelBoard.Size = new System.Drawing.Size(678, 580);
            this.panelBoard.TabIndex = 1;
            this.panelBoard.Paint += new System.Windows.Forms.PaintEventHandler(this.panelBoard_Paint);
            this.panelBoard.MouseClick += new System.Windows.Forms.MouseEventHandler(this.panelBoard_MouseClick);
            // 
            // openFENDialog
            // 
            this.openFENDialog.Filter = "象棋局面文件(*.FEN)|*.FEN";
            this.openFENDialog.Title = "从局面文件导入";
            // 
            // saveFENDialog
            // 
            this.saveFENDialog.Filter = "象棋局面文件(*.FEN)|*.FEN";
            this.saveFENDialog.Title = "导出到局面文件";
            // 
            // menuCopyFEN
            // 
            this.menuCopyFEN.Name = "menuCopyFEN";
            this.menuCopyFEN.Size = new System.Drawing.Size(188, 22);
            this.menuCopyFEN.Text = "复制局面代码(&C)";
            this.menuCopyFEN.Click += new System.EventHandler(this.menuCopyFEN_Click);
            // 
            // menuPasteFEN
            // 
            this.menuPasteFEN.Name = "menuPasteFEN";
            this.menuPasteFEN.Size = new System.Drawing.Size(188, 22);
            this.menuPasteFEN.Text = "粘贴局面代码(&P)";
            this.menuPasteFEN.Click += new System.EventHandler(this.menuPasteFEN_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(185, 6);
            // 
            // listMove
            // 
            this.listMove.FormattingEnabled = true;
            this.listMove.ItemHeight = 12;
            this.listMove.Items.AddRange(new object[] {
            "==开始=="});
            this.listMove.Location = new System.Drawing.Point(531, 24);
            this.listMove.Name = "listMove";
            this.listMove.Size = new System.Drawing.Size(103, 292);
            this.listMove.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(529, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "着法列表";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Black;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(678, 604);
            this.Controls.Add(this.panelBoard);
            this.Controls.Add(this.menuStrip1);
            this.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "象棋小巫师";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panelBoard.ResumeLayout(false);
            this.panelBoard.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ImageList ilCPieces;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 文件ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NewGameMenu;
        private System.Windows.Forms.Panel panelBoard;
        private System.Windows.Forms.ToolStripMenuItem 电脑ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuAIRed;
        private System.Windows.Forms.ToolStripMenuItem menuAIBlack;
        private System.Windows.Forms.ToolStripMenuItem menuPonder;
        private System.Windows.Forms.ToolStripMenuItem 帮助HToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuAboutEngine;
        private System.Windows.Forms.ToolStripMenuItem 局面ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuFlipBoard;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem menuEditFEN;
        private System.Windows.Forms.ToolStripMenuItem menuLoadFEN;
        private System.Windows.Forms.ToolStripMenuItem menuSaveFEN;
        private System.Windows.Forms.OpenFileDialog openFENDialog;
        private System.Windows.Forms.SaveFileDialog saveFENDialog;
        private System.Windows.Forms.ToolStripMenuItem menuPasteFEN;
        private System.Windows.Forms.ToolStripMenuItem menuCopyFEN;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listMove;
    }
}

