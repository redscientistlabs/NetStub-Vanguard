namespace NetStub
{
    partial class StubForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StubForm));
            this.pnTarget = new System.Windows.Forms.Panel();
            this.lbCaveCount = new System.Windows.Forms.Label();
            this.tbProcName = new System.Windows.Forms.TextBox();
            this.lbPID = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnFindCaves = new System.Windows.Forms.Button();
            this.btnStartClient = new System.Windows.Forms.Button();
            this.btnPayload = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.tbClientAddr = new System.Windows.Forms.TextBox();
            this.btnTargetSettings = new System.Windows.Forms.Button();
            this.pnSideBar = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.lbTargetStatus = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnRefreshDomains = new System.Windows.Forms.Button();
            this.cbMode = new System.Windows.Forms.ComboBox();
            this.cbOverrideHandlers = new System.Windows.Forms.CheckBox();
            this.tbDomainWhitelist = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbDomainBlacklist = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.pnTarget.SuspendLayout();
            this.pnSideBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnTarget
            // 
            this.pnTarget.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnTarget.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.pnTarget.Controls.Add(this.lbCaveCount);
            this.pnTarget.Controls.Add(this.tbProcName);
            this.pnTarget.Controls.Add(this.lbPID);
            this.pnTarget.Controls.Add(this.label1);
            this.pnTarget.Controls.Add(this.btnFindCaves);
            this.pnTarget.Controls.Add(this.btnStartClient);
            this.pnTarget.Controls.Add(this.btnPayload);
            this.pnTarget.Controls.Add(this.btnConnect);
            this.pnTarget.Controls.Add(this.label3);
            this.pnTarget.Controls.Add(this.tbClientAddr);
            this.pnTarget.Location = new System.Drawing.Point(129, 49);
            this.pnTarget.Name = "pnTarget";
            this.pnTarget.Size = new System.Drawing.Size(463, 122);
            this.pnTarget.TabIndex = 13;
            this.pnTarget.Tag = "color:dark1";
            // 
            // lbCaveCount
            // 
            this.lbCaveCount.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbCaveCount.AutoSize = true;
            this.lbCaveCount.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lbCaveCount.ForeColor = System.Drawing.Color.White;
            this.lbCaveCount.Location = new System.Drawing.Point(112, 100);
            this.lbCaveCount.Name = "lbCaveCount";
            this.lbCaveCount.Size = new System.Drawing.Size(97, 13);
            this.lbCaveCount.TabIndex = 185;
            this.lbCaveCount.Text = "Available Caves: 0";
            // 
            // tbProcName
            // 
            this.tbProcName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbProcName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(32)))));
            this.tbProcName.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tbProcName.ForeColor = System.Drawing.Color.White;
            this.tbProcName.Location = new System.Drawing.Point(129, 32);
            this.tbProcName.Multiline = true;
            this.tbProcName.Name = "tbProcName";
            this.tbProcName.Size = new System.Drawing.Size(134, 21);
            this.tbProcName.TabIndex = 184;
            this.tbProcName.Tag = "color:dark2";
            this.tbProcName.TextChanged += new System.EventHandler(this.tbProcName_TextChanged);
            // 
            // lbPID
            // 
            this.lbPID.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbPID.AutoSize = true;
            this.lbPID.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lbPID.ForeColor = System.Drawing.Color.White;
            this.lbPID.Location = new System.Drawing.Point(126, 56);
            this.lbPID.Name = "lbPID";
            this.lbPID.Size = new System.Drawing.Size(42, 13);
            this.lbPID.TabIndex = 183;
            this.lbPID.Text = "(PID: 0)";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(78, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 182;
            this.label1.Text = "Process";
            // 
            // btnFindCaves
            // 
            this.btnFindCaves.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFindCaves.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.btnFindCaves.FlatAppearance.BorderSize = 0;
            this.btnFindCaves.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFindCaves.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnFindCaves.ForeColor = System.Drawing.Color.White;
            this.btnFindCaves.Location = new System.Drawing.Point(3, 95);
            this.btnFindCaves.Name = "btnFindCaves";
            this.btnFindCaves.Size = new System.Drawing.Size(103, 23);
            this.btnFindCaves.TabIndex = 43;
            this.btnFindCaves.TabStop = false;
            this.btnFindCaves.Tag = "color:dark2";
            this.btnFindCaves.Text = "Find Code Caves";
            this.btnFindCaves.UseVisualStyleBackColor = false;
            this.btnFindCaves.Click += new System.EventHandler(this.btnFindCaves_Click);
            // 
            // btnStartClient
            // 
            this.btnStartClient.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartClient.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.btnStartClient.FlatAppearance.BorderSize = 0;
            this.btnStartClient.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartClient.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnStartClient.ForeColor = System.Drawing.Color.White;
            this.btnStartClient.Location = new System.Drawing.Point(348, 32);
            this.btnStartClient.Name = "btnStartClient";
            this.btnStartClient.Size = new System.Drawing.Size(103, 23);
            this.btnStartClient.TabIndex = 180;
            this.btnStartClient.TabStop = false;
            this.btnStartClient.Tag = "color:dark2";
            this.btnStartClient.Text = "Start";
            this.btnStartClient.UseVisualStyleBackColor = false;
            this.btnStartClient.Visible = false;
            this.btnStartClient.Click += new System.EventHandler(this.btnStartClient_Click);
            // 
            // btnPayload
            // 
            this.btnPayload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPayload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.btnPayload.FlatAppearance.BorderSize = 0;
            this.btnPayload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPayload.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnPayload.ForeColor = System.Drawing.Color.White;
            this.btnPayload.Location = new System.Drawing.Point(348, 90);
            this.btnPayload.Name = "btnPayload";
            this.btnPayload.Size = new System.Drawing.Size(103, 23);
            this.btnPayload.TabIndex = 179;
            this.btnPayload.TabStop = false;
            this.btnPayload.Tag = "color:dark2";
            this.btnPayload.Text = "Inject Payload";
            this.btnPayload.UseVisualStyleBackColor = false;
            this.btnPayload.Visible = false;
            this.btnPayload.Click += new System.EventHandler(this.btnPayload_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.btnConnect.FlatAppearance.BorderSize = 0;
            this.btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConnect.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnConnect.ForeColor = System.Drawing.Color.White;
            this.btnConnect.Location = new System.Drawing.Point(348, 61);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(103, 23);
            this.btnConnect.TabIndex = 179;
            this.btnConnect.TabStop = false;
            this.btnConnect.Tag = "color:dark2";
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = false;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(112, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 13);
            this.label3.TabIndex = 178;
            this.label3.Text = "Client Address";
            // 
            // tbClientAddr
            // 
            this.tbClientAddr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbClientAddr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(32)))));
            this.tbClientAddr.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tbClientAddr.ForeColor = System.Drawing.Color.White;
            this.tbClientAddr.Location = new System.Drawing.Point(199, 5);
            this.tbClientAddr.Multiline = true;
            this.tbClientAddr.Name = "tbClientAddr";
            this.tbClientAddr.Size = new System.Drawing.Size(252, 21);
            this.tbClientAddr.TabIndex = 177;
            this.tbClientAddr.Tag = "color:dark2";
            this.tbClientAddr.TextChanged += new System.EventHandler(this.tbClientAddr_TextChanged);
            // 
            // btnTargetSettings
            // 
            this.btnTargetSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTargetSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnTargetSettings.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnTargetSettings.FlatAppearance.BorderSize = 0;
            this.btnTargetSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTargetSettings.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnTargetSettings.ForeColor = System.Drawing.Color.OrangeRed;
            this.btnTargetSettings.Image = ((System.Drawing.Image)(resources.GetObject("btnTargetSettings.Image")));
            this.btnTargetSettings.Location = new System.Drawing.Point(560, 13);
            this.btnTargetSettings.Name = "btnTargetSettings";
            this.btnTargetSettings.Size = new System.Drawing.Size(32, 32);
            this.btnTargetSettings.TabIndex = 172;
            this.btnTargetSettings.TabStop = false;
            this.btnTargetSettings.Tag = "color:dark1";
            this.btnTargetSettings.UseVisualStyleBackColor = false;
            // 
            // pnSideBar
            // 
            this.pnSideBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.pnSideBar.Controls.Add(this.label4);
            this.pnSideBar.Controls.Add(this.lbTargetStatus);
            this.pnSideBar.Controls.Add(this.label2);
            this.pnSideBar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnSideBar.Location = new System.Drawing.Point(0, 0);
            this.pnSideBar.Name = "pnSideBar";
            this.pnSideBar.Size = new System.Drawing.Size(118, 371);
            this.pnSideBar.TabIndex = 174;
            this.pnSideBar.Tag = "color:dark3";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(9, 191);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(102, 171);
            this.label4.TabIndex = 124;
            this.label4.Tag = "";
            this.label4.Text = "Don\'t be stupid. Don\'t corrupt OS/kernel processes, don\'t corrupt anything that u" +
    "ses online services.";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // lbTargetStatus
            // 
            this.lbTargetStatus.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lbTargetStatus.ForeColor = System.Drawing.Color.White;
            this.lbTargetStatus.Location = new System.Drawing.Point(7, 37);
            this.lbTargetStatus.Name = "lbTargetStatus";
            this.lbTargetStatus.Size = new System.Drawing.Size(110, 44);
            this.lbTargetStatus.TabIndex = 123;
            this.lbTargetStatus.Text = "No target selected";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI Semibold", 10F);
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(8, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 19);
            this.label2.TabIndex = 122;
            this.label2.Text = "Status";
            // 
            // btnRefreshDomains
            // 
            this.btnRefreshDomains.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefreshDomains.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.btnRefreshDomains.FlatAppearance.BorderSize = 0;
            this.btnRefreshDomains.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefreshDomains.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnRefreshDomains.ForeColor = System.Drawing.Color.White;
            this.btnRefreshDomains.Location = new System.Drawing.Point(477, 327);
            this.btnRefreshDomains.Name = "btnRefreshDomains";
            this.btnRefreshDomains.Size = new System.Drawing.Size(103, 23);
            this.btnRefreshDomains.TabIndex = 43;
            this.btnRefreshDomains.TabStop = false;
            this.btnRefreshDomains.Tag = "color:dark2";
            this.btnRefreshDomains.Text = "Refresh Domains";
            this.btnRefreshDomains.UseVisualStyleBackColor = false;
            this.btnRefreshDomains.Visible = false;
            this.btnRefreshDomains.Click += new System.EventHandler(this.BtnRefreshDomains_Click);
            // 
            // cbMode
            // 
            this.cbMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(32)))));
            this.cbMode.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.cbMode.ForeColor = System.Drawing.Color.White;
            this.cbMode.FormattingEnabled = true;
            this.cbMode.Items.AddRange(new object[] {
            "PS4",
            "PowerPC Mac"});
            this.cbMode.Location = new System.Drawing.Point(129, 13);
            this.cbMode.Name = "cbMode";
            this.cbMode.Size = new System.Drawing.Size(281, 21);
            this.cbMode.TabIndex = 175;
            this.cbMode.SelectedIndexChanged += new System.EventHandler(this.cbMode_SelectedIndexChanged);
            // 
            // cbOverrideHandlers
            // 
            this.cbOverrideHandlers.AutoSize = true;
            this.cbOverrideHandlers.ForeColor = System.Drawing.Color.White;
            this.cbOverrideHandlers.Location = new System.Drawing.Point(129, 177);
            this.cbOverrideHandlers.Name = "cbOverrideHandlers";
            this.cbOverrideHandlers.Size = new System.Drawing.Size(161, 17);
            this.cbOverrideHandlers.TabIndex = 176;
            this.cbOverrideHandlers.Text = "Override Exception Handlers";
            this.cbOverrideHandlers.UseVisualStyleBackColor = true;
            this.cbOverrideHandlers.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // tbDomainWhitelist
            // 
            this.tbDomainWhitelist.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDomainWhitelist.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(32)))));
            this.tbDomainWhitelist.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tbDomainWhitelist.ForeColor = System.Drawing.Color.White;
            this.tbDomainWhitelist.Location = new System.Drawing.Point(132, 300);
            this.tbDomainWhitelist.Multiline = true;
            this.tbDomainWhitelist.Name = "tbDomainWhitelist";
            this.tbDomainWhitelist.Size = new System.Drawing.Size(320, 50);
            this.tbDomainWhitelist.TabIndex = 186;
            this.tbDomainWhitelist.Tag = "color:dark2";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(129, 284);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 13);
            this.label5.TabIndex = 186;
            this.label5.Text = "Domain Whitelist";
            // 
            // tbDomainBlacklist
            // 
            this.tbDomainBlacklist.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDomainBlacklist.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(32)))));
            this.tbDomainBlacklist.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.tbDomainBlacklist.ForeColor = System.Drawing.Color.White;
            this.tbDomainBlacklist.Location = new System.Drawing.Point(132, 231);
            this.tbDomainBlacklist.Multiline = true;
            this.tbDomainBlacklist.Name = "tbDomainBlacklist";
            this.tbDomainBlacklist.Size = new System.Drawing.Size(320, 50);
            this.tbDomainBlacklist.TabIndex = 186;
            this.tbDomainBlacklist.Tag = "color:dark2";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label6.ForeColor = System.Drawing.Color.White;
            this.label6.Location = new System.Drawing.Point(129, 215);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(91, 13);
            this.label6.TabIndex = 186;
            this.label6.Text = "Domain Blacklist";
            // 
            // StubForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(612, 371);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tbDomainBlacklist);
            this.Controls.Add(this.tbDomainWhitelist);
            this.Controls.Add(this.cbOverrideHandlers);
            this.Controls.Add(this.cbMode);
            this.Controls.Add(this.btnTargetSettings);
            this.Controls.Add(this.btnRefreshDomains);
            this.Controls.Add(this.pnSideBar);
            this.Controls.Add(this.pnTarget);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(516, 300);
            this.Name = "StubForm";
            this.Tag = "color:dark2";
            this.Text = "NetStub";
            this.Load += new System.EventHandler(this.StubForm_Load_1);
            this.pnTarget.ResumeLayout(false);
            this.pnTarget.PerformLayout();
            this.pnSideBar.ResumeLayout(false);
            this.pnSideBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Panel pnTarget;
        public System.Windows.Forms.Button btnTargetSettings;
        private System.Windows.Forms.Panel pnSideBar;
        public System.Windows.Forms.Label lbTargetStatus;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnRefreshDomains;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.TextBox tbClientAddr;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnStartClient;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnPayload;
        private System.Windows.Forms.Label lbPID;
        public System.Windows.Forms.TextBox tbProcName;
        private System.Windows.Forms.ComboBox cbMode;
        private System.Windows.Forms.CheckBox cbOverrideHandlers;
        private System.Windows.Forms.Button btnFindCaves;
        public System.Windows.Forms.Label lbCaveCount;
        public System.Windows.Forms.TextBox tbDomainWhitelist;
        public System.Windows.Forms.Label label5;
        public System.Windows.Forms.TextBox tbDomainBlacklist;
        public System.Windows.Forms.Label label6;
    }
}

