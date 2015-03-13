namespace OSQuoteTester
{
    partial class ClientMain
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnInitialize = new System.Windows.Forms.Button();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassWord = new System.Windows.Forms.TextBox();
            this.lblAccount = new System.Windows.Forms.Label();
            this.txtAccount = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnGo = new System.Windows.Forms.Button();
            this.lblMessage = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.gridStocks = new System.Windows.Forms.DataGridView();
            this.btnQueryStocks = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtStocks = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.lblBest5Count = new System.Windows.Forms.Label();
            this.listTicks = new System.Windows.Forms.ListBox();
            this.gridBest5Bid = new System.Windows.Forms.DataGridView();
            this.gridBest5Ask = new System.Windows.Forms.DataGridView();
            this.btnTick = new System.Windows.Forms.Button();
            this.txtTick = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.listOverseaProducts = new System.Windows.Forms.ListBox();
            this.btnOverseaProducts = new System.Windows.Forms.Button();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.boxKLineType = new System.Windows.Forms.ComboBox();
            this.listKLine = new System.Windows.Forms.ListBox();
            this.btnKLine = new System.Windows.Forms.Button();
            this.txtKLine = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblConnect = new System.Windows.Forms.Label();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnServerTime = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblServerTime = new System.Windows.Forms.Label();
            this.timer2 = new System.Timers.Timer();
            this.groupBox3.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridStocks)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridBest5Bid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridBest5Ask)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            //((System.ComponentModel.ISupportInitialize)(this.timer2)).BeginInit();
            this.SuspendLayout();
            // 
            // btnInitialize
            // 
            this.btnInitialize.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnInitialize.Location = new System.Drawing.Point(338, 15);
            this.btnInitialize.Margin = new System.Windows.Forms.Padding(6);
            this.btnInitialize.Name = "btnInitialize";
            this.btnInitialize.Size = new System.Drawing.Size(171, 65);
            this.btnInitialize.TabIndex = 18;
            this.btnInitialize.Text = "Initialize";
            this.btnInitialize.UseVisualStyleBackColor = true;
            this.btnInitialize.Click += new System.EventHandler(this.btnInitialize_Click);
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = new System.Drawing.Font("Verdana", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblPassword.Location = new System.Drawing.Point(11, 58);
            this.lblPassword.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(75, 15);
            this.lblPassword.TabIndex = 20;
            this.lblPassword.Text = "Password：";
            // 
            // txtPassWord
            // 
            this.txtPassWord.Location = new System.Drawing.Point(103, 53);
            this.txtPassWord.Margin = new System.Windows.Forms.Padding(6);
            this.txtPassWord.Name = "txtPassWord";
            this.txtPassWord.PasswordChar = '*';
            this.txtPassWord.Size = new System.Drawing.Size(203, 24);
            this.txtPassWord.TabIndex = 17;
            this.txtPassWord.Text = "123456";
            // 
            // lblAccount
            // 
            this.lblAccount.AutoSize = true;
            this.lblAccount.Font = new System.Drawing.Font("Verdana", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblAccount.Location = new System.Drawing.Point(11, 23);
            this.lblAccount.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAccount.Name = "lblAccount";
            this.lblAccount.Size = new System.Drawing.Size(69, 15);
            this.lblAccount.TabIndex = 19;
            this.lblAccount.Text = "Account：";
            // 
            // txtAccount
            // 
            this.txtAccount.Font = new System.Drawing.Font("Verdana", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.txtAccount.Location = new System.Drawing.Point(103, 15);
            this.txtAccount.Margin = new System.Windows.Forms.Padding(6);
            this.txtAccount.Name = "txtAccount";
            this.txtAccount.Size = new System.Drawing.Size(203, 25);
            this.txtAccount.TabIndex = 16;
            this.txtAccount.Text = "A12345678";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnGo);
            this.groupBox3.Controls.Add(this.lblMessage);
            this.groupBox3.Location = new System.Drawing.Point(338, 83);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(611, 51);
            this.groupBox3.TabIndex = 22;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "訊息";
            // 
            // btnGo
            // 
            this.btnGo.Location = new System.Drawing.Point(222, 3);
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(87, 48);
            this.btnGo.TabIndex = 29;
            this.btnGo.Text = "Go";
            this.btnGo.UseVisualStyleBackColor = true;
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // lblMessage
            // 
            this.lblMessage.AutoSize = true;
            this.lblMessage.Location = new System.Drawing.Point(16, 22);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(35, 14);
            this.lblMessage.TabIndex = 0;
            this.lblMessage.Text = "訊息";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(14, 96);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(93, 29);
            this.btnConnect.TabIndex = 23;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(14, 140);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(939, 378);
            this.tabControl1.TabIndex = 24;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.gridStocks);
            this.tabPage1.Controls.Add(this.btnQueryStocks);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.txtStocks);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new System.Drawing.Point(4, 23);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(931, 351);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "報價";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // gridStocks
            // 
            this.gridStocks.AllowUserToAddRows = false;
            this.gridStocks.AllowUserToDeleteRows = false;
            this.gridStocks.AllowUserToResizeRows = false;
            this.gridStocks.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.AppWorkspace;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridStocks.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gridStocks.Location = new System.Drawing.Point(6, 54);
            this.gridStocks.Name = "gridStocks";
            this.gridStocks.ReadOnly = true;
            this.gridStocks.RowHeadersVisible = false;
            this.gridStocks.RowTemplate.Height = 24;
            this.gridStocks.Size = new System.Drawing.Size(919, 291);
            this.gridStocks.TabIndex = 8;
            this.gridStocks.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.gridStocks_CellPainting);
            // 
            // btnQueryStocks
            // 
            this.btnQueryStocks.Location = new System.Drawing.Point(358, 6);
            this.btnQueryStocks.Name = "btnQueryStocks";
            this.btnQueryStocks.Size = new System.Drawing.Size(133, 23);
            this.btnQueryStocks.TabIndex = 7;
            this.btnQueryStocks.Text = "查詢";
            this.btnQueryStocks.UseVisualStyleBackColor = true;
            this.btnQueryStocks.Click += new System.EventHandler(this.btnQueryStocks_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(334, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(339, 14);
            this.label2.TabIndex = 6;
            this.label2.Text = "格式 [ 交易代碼,商品報價代碼 ] ( 多筆以 井號{#}區隔 )";
            // 
            // txtStocks
            // 
            this.txtStocks.Location = new System.Drawing.Point(85, 23);
            this.txtStocks.Name = "txtStocks";
            this.txtStocks.Size = new System.Drawing.Size(243, 24);
            this.txtStocks.TabIndex = 5;
            this.txtStocks.Text = "CBT,ZB1112#HKF,HSI1112";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 14);
            this.label1.TabIndex = 4;
            this.label1.Text = "商品代碼";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.listBox1);
            this.tabPage2.Controls.Add(this.lblBest5Count);
            this.tabPage2.Controls.Add(this.listTicks);
            this.tabPage2.Controls.Add(this.gridBest5Bid);
            this.tabPage2.Controls.Add(this.gridBest5Ask);
            this.tabPage2.Controls.Add(this.btnTick);
            this.tabPage2.Controls.Add(this.txtTick);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Location = new System.Drawing.Point(4, 23);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(931, 351);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Ticks";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // listBox1
            // 
            this.listBox1.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 16;
            this.listBox1.Location = new System.Drawing.Point(26, 234);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(513, 100);
            this.listBox1.TabIndex = 13;
            // 
            // lblBest5Count
            // 
            this.lblBest5Count.AutoSize = true;
            this.lblBest5Count.Location = new System.Drawing.Point(641, 21);
            this.lblBest5Count.Name = "lblBest5Count";
            this.lblBest5Count.Size = new System.Drawing.Size(41, 14);
            this.lblBest5Count.TabIndex = 12;
            this.lblBest5Count.Text = "label5";
            // 
            // listTicks
            // 
            this.listTicks.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.listTicks.FormattingEnabled = true;
            this.listTicks.ItemHeight = 16;
            this.listTicks.Location = new System.Drawing.Point(26, 54);
            this.listTicks.Name = "listTicks";
            this.listTicks.Size = new System.Drawing.Size(418, 164);
            this.listTicks.TabIndex = 11;
            // 
            // gridBest5Bid
            // 
            this.gridBest5Bid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridBest5Bid.Location = new System.Drawing.Point(703, 51);
            this.gridBest5Bid.MultiSelect = false;
            this.gridBest5Bid.Name = "gridBest5Bid";
            this.gridBest5Bid.ReadOnly = true;
            this.gridBest5Bid.RowHeadersVisible = false;
            this.gridBest5Bid.RowTemplate.Height = 24;
            this.gridBest5Bid.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.gridBest5Bid.Size = new System.Drawing.Size(131, 172);
            this.gridBest5Bid.TabIndex = 9;
            // 
            // gridBest5Ask
            // 
            this.gridBest5Ask.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridBest5Ask.Location = new System.Drawing.Point(541, 51);
            this.gridBest5Ask.MultiSelect = false;
            this.gridBest5Ask.Name = "gridBest5Ask";
            this.gridBest5Ask.ReadOnly = true;
            this.gridBest5Ask.RowHeadersVisible = false;
            this.gridBest5Ask.RowTemplate.Height = 24;
            this.gridBest5Ask.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.gridBest5Ask.Size = new System.Drawing.Size(131, 172);
            this.gridBest5Ask.TabIndex = 8;
            // 
            // btnTick
            // 
            this.btnTick.Location = new System.Drawing.Point(209, 10);
            this.btnTick.Name = "btnTick";
            this.btnTick.Size = new System.Drawing.Size(102, 34);
            this.btnTick.TabIndex = 6;
            this.btnTick.Text = "查詢";
            this.btnTick.UseVisualStyleBackColor = true;
            this.btnTick.Click += new System.EventHandler(this.btnTick_Click);
            // 
            // txtTick
            // 
            this.txtTick.Location = new System.Drawing.Point(95, 18);
            this.txtTick.Name = "txtTick";
            this.txtTick.Size = new System.Drawing.Size(100, 24);
            this.txtTick.TabIndex = 5;
            this.txtTick.Text = "CBT,YM1112";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 14);
            this.label3.TabIndex = 4;
            this.label3.Text = "商品代碼";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.listOverseaProducts);
            this.tabPage3.Controls.Add(this.btnOverseaProducts);
            this.tabPage3.Location = new System.Drawing.Point(4, 23);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(931, 351);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "商品";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // listOverseaProducts
            // 
            this.listOverseaProducts.FormattingEnabled = true;
            this.listOverseaProducts.Location = new System.Drawing.Point(68, 112);
            this.listOverseaProducts.Name = "listOverseaProducts";
            this.listOverseaProducts.Size = new System.Drawing.Size(776, 199);
            this.listOverseaProducts.TabIndex = 1;
            // 
            // btnOverseaProducts
            // 
            this.btnOverseaProducts.Location = new System.Drawing.Point(320, 18);
            this.btnOverseaProducts.Name = "btnOverseaProducts";
            this.btnOverseaProducts.Size = new System.Drawing.Size(261, 63);
            this.btnOverseaProducts.TabIndex = 0;
            this.btnOverseaProducts.Text = "GetOverseaProducts";
            this.btnOverseaProducts.UseVisualStyleBackColor = true;
            this.btnOverseaProducts.Click += new System.EventHandler(this.btnOverseaProducts_Click);
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.boxKLineType);
            this.tabPage4.Controls.Add(this.listKLine);
            this.tabPage4.Controls.Add(this.btnKLine);
            this.tabPage4.Controls.Add(this.txtKLine);
            this.tabPage4.Controls.Add(this.label4);
            this.tabPage4.Location = new System.Drawing.Point(4, 23);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(931, 351);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "KLine";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // boxKLineType
            // 
            this.boxKLineType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.boxKLineType.FormattingEnabled = true;
            this.boxKLineType.Items.AddRange(new object[] {
            "分鐘線",
            "日線",
            "週線",
            "月線"});
            this.boxKLineType.Location = new System.Drawing.Point(219, 22);
            this.boxKLineType.Name = "boxKLineType";
            this.boxKLineType.Size = new System.Drawing.Size(121, 21);
            this.boxKLineType.TabIndex = 10;
            // 
            // listKLine
            // 
            this.listKLine.FormattingEnabled = true;
            this.listKLine.Location = new System.Drawing.Point(54, 81);
            this.listKLine.Name = "listKLine";
            this.listKLine.Size = new System.Drawing.Size(776, 238);
            this.listKLine.TabIndex = 9;
            // 
            // btnKLine
            // 
            this.btnKLine.Location = new System.Drawing.Point(374, 11);
            this.btnKLine.Name = "btnKLine";
            this.btnKLine.Size = new System.Drawing.Size(171, 40);
            this.btnKLine.TabIndex = 8;
            this.btnKLine.Text = "RequestLKine";
            this.btnKLine.UseVisualStyleBackColor = true;
            this.btnKLine.Click += new System.EventHandler(this.btnKLine_Click);
            // 
            // txtKLine
            // 
            this.txtKLine.Location = new System.Drawing.Point(95, 22);
            this.txtKLine.Name = "txtKLine";
            this.txtKLine.Size = new System.Drawing.Size(100, 24);
            this.txtKLine.TabIndex = 7;
            this.txtKLine.Text = "CME,ES_1112";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(63, 14);
            this.label4.TabIndex = 6;
            this.label4.Text = "商品代碼";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lblConnect);
            this.groupBox2.Location = new System.Drawing.Point(528, 15);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(71, 62);
            this.groupBox2.TabIndex = 25;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "訊號";
            // 
            // lblConnect
            // 
            this.lblConnect.AutoSize = true;
            this.lblConnect.Font = new System.Drawing.Font("Verdana", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblConnect.Location = new System.Drawing.Point(19, 24);
            this.lblConnect.Name = "lblConnect";
            this.lblConnect.Size = new System.Drawing.Size(32, 22);
            this.lblConnect.TabIndex = 0;
            this.lblConnect.Text = "●";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Location = new System.Drawing.Point(113, 96);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(94, 29);
            this.btnDisconnect.TabIndex = 26;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // btnServerTime
            // 
            this.btnServerTime.Location = new System.Drawing.Point(219, 96);
            this.btnServerTime.Name = "btnServerTime";
            this.btnServerTime.Size = new System.Drawing.Size(87, 29);
            this.btnServerTime.TabIndex = 27;
            this.btnServerTime.Text = "ServerTime";
            this.btnServerTime.UseVisualStyleBackColor = true;
            this.btnServerTime.Click += new System.EventHandler(this.btnServerTime_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblServerTime);
            this.groupBox1.Location = new System.Drawing.Point(641, 15);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(266, 62);
            this.groupBox1.TabIndex = 28;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "收盤剩下";
            // 
            // lblServerTime
            // 
            this.lblServerTime.AutoSize = true;
            this.lblServerTime.Font = new System.Drawing.Font("Verdana", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblServerTime.Location = new System.Drawing.Point(19, 24);
            this.lblServerTime.Name = "lblServerTime";
            this.lblServerTime.Size = new System.Drawing.Size(83, 19);
            this.lblServerTime.TabIndex = 0;
            this.lblServerTime.Text = "--：--：--";
            // 
            // timer2
            // 
            //this.timer2.SynchronizingObject = this;
            this.timer2.Elapsed += new System.Timers.ElapsedEventHandler(this.timer2_Tick);
            // 
            // ClientMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(961, 530);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnServerTime);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.btnInitialize);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.txtPassWord);
            this.Controls.Add(this.lblAccount);
            this.Controls.Add(this.txtAccount);
            this.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.Name = "ClientMain";
            this.Text = "OSQuote";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.Main_Load);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridStocks)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridBest5Bid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridBest5Ask)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timer2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnInitialize;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassWord;
        private System.Windows.Forms.Label lblAccount;
        private System.Windows.Forms.TextBox txtAccount;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button btnQueryStocks;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtStocks;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblConnect;
        private System.Windows.Forms.DataGridView gridStocks;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnTick;
        private System.Windows.Forms.TextBox txtTick;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView gridBest5Bid;
        private System.Windows.Forms.DataGridView gridBest5Ask;
        private System.Windows.Forms.ListBox listTicks;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.ListBox listOverseaProducts;
        private System.Windows.Forms.Button btnOverseaProducts;
        private System.Windows.Forms.Button btnServerTime;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblServerTime;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.Button btnKLine;
        private System.Windows.Forms.TextBox txtKLine;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox listKLine;
        private System.Windows.Forms.ComboBox boxKLineType;
        private System.Windows.Forms.Label lblBest5Count;
        private System.Timers.Timer timer2;
        private System.Windows.Forms.Button btnGo;
        private System.Windows.Forms.ListBox listBox1;
    }
}

