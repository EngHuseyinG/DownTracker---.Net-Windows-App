namespace DownTracker.Views
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
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblDragDrop = new System.Windows.Forms.Label();
            this.lblFilePath = new System.Windows.Forms.Label();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.pnlLeft = new System.Windows.Forms.Panel();
            this.tvStations = new System.Windows.Forms.TreeView();
            this.lblStationsTitle = new System.Windows.Forms.Label();
            this.pnlFilters = new System.Windows.Forms.Panel();
            this.rbFilterEmpty = new System.Windows.Forms.RadioButton();
            this.rbFilterAlarmed = new System.Windows.Forms.RadioButton();
            this.rbFilterAll = new System.Windows.Forms.RadioButton();
            this.lblFiltersTitle = new System.Windows.Forms.Label();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tpDowntimes = new System.Windows.Forms.TabPage();
            this.tpCycles = new System.Windows.Forms.TabPage();
            this.tpRawLogs = new System.Windows.Forms.TabPage();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();

            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlLeft.SuspendLayout();
            this.pnlFilters.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.tpDowntimes.SuspendLayout();
            this.tpCycles.SuspendLayout();
            this.tpRawLogs.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlTop
            // 
            this.pnlTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(31)))), ((int)(((byte)(31)))));
            this.pnlTop.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlTop.Controls.Add(this.lblDragDrop);
            this.pnlTop.Controls.Add(this.lblFilePath);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Padding = new System.Windows.Forms.Padding(10);
            this.pnlTop.Size = new System.Drawing.Size(1200, 100);
            this.pnlTop.TabIndex = 0;
            // 
            // lblDragDrop
            // 
            this.lblDragDrop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblDragDrop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDragDrop.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblDragDrop.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(229)))), ((int)(((byte)(255)))));
            this.lblDragDrop.Location = new System.Drawing.Point(10, 10);
            this.lblDragDrop.Name = "lblDragDrop";
            this.lblDragDrop.Size = new System.Drawing.Size(1178, 58);
            this.lblDragDrop.TabIndex = 0;
            this.lblDragDrop.Text = "DURUŞ VE ALARM ANALİZİ İÇİN GRAFANA SORGU ÇIKTISINI (DOWNLOAD CSV SEÇENEĞİNİ KULLANMADAN) DOĞRUDAN BURAYA SÜRÜKLEYİN VEYA TIKLAYIN";
            this.lblDragDrop.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblDragDrop.Click += new System.EventHandler(this.lblDragDrop_Click);
            // 
            // lblFilePath
            // 
            this.lblFilePath.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblFilePath.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.lblFilePath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.lblFilePath.Location = new System.Drawing.Point(10, 68);
            this.lblFilePath.Name = "lblFilePath";
            this.lblFilePath.Size = new System.Drawing.Size(1178, 20);
            this.lblFilePath.TabIndex = 1;
            this.lblFilePath.Text = "Yüklü Dosya: Yok";
            this.lblFilePath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 100);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.pnlLeft);
            this.splitMain.Panel1MinSize = 250;
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.tabMain);
            this.splitMain.Size = new System.Drawing.Size(1200, 678);
            this.splitMain.SplitterDistance = 300;
            this.splitMain.SplitterWidth = 4;
            this.splitMain.TabIndex = 1;
            // 
            // pnlLeft
            // 
            this.pnlLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.pnlLeft.Controls.Add(this.tvStations);
            this.pnlLeft.Controls.Add(this.lblStationsTitle);
            this.pnlLeft.Controls.Add(this.pnlFilters);
            this.pnlLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLeft.Location = new System.Drawing.Point(0, 0);
            this.pnlLeft.Name = "pnlLeft";
            this.pnlLeft.Padding = new System.Windows.Forms.Padding(10);
            this.pnlLeft.Size = new System.Drawing.Size(300, 678);
            this.pnlLeft.TabIndex = 0;
            // 
            // tvStations
            // 
            this.tvStations.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.tvStations.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tvStations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvStations.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.tvStations.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.tvStations.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.tvStations.Location = new System.Drawing.Point(10, 35);
            this.tvStations.Name = "tvStations";
            this.tvStations.Size = new System.Drawing.Size(280, 483);
            this.tvStations.TabIndex = 1;
            this.tvStations.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvStations_AfterSelect);
            // 
            // lblStationsTitle
            // 
            this.lblStationsTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStationsTitle.Font = new System.Drawing.Font("Segoe UI Black", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblStationsTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.lblStationsTitle.Location = new System.Drawing.Point(10, 10);
            this.lblStationsTitle.Name = "lblStationsTitle";
            this.lblStationsTitle.Size = new System.Drawing.Size(280, 25);
            this.lblStationsTitle.TabIndex = 0;
            this.lblStationsTitle.Text = "İSTASYON / ROBOT AĞACI";
            this.lblStationsTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlFilters
            // 
            this.pnlFilters.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.pnlFilters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlFilters.Controls.Add(this.rbFilterEmpty);
            this.pnlFilters.Controls.Add(this.rbFilterAlarmed);
            this.pnlFilters.Controls.Add(this.rbFilterAll);
            this.pnlFilters.Controls.Add(this.lblFiltersTitle);
            this.pnlFilters.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFilters.Location = new System.Drawing.Point(10, 518);
            this.pnlFilters.Name = "pnlFilters";
            this.pnlFilters.Padding = new System.Windows.Forms.Padding(10, 5, 10, 10);
            this.pnlFilters.Size = new System.Drawing.Size(280, 150);
            this.pnlFilters.TabIndex = 2;
            // 
            // rbFilterEmpty
            // 
            this.rbFilterEmpty.Dock = System.Windows.Forms.DockStyle.Top;
            this.rbFilterEmpty.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbFilterEmpty.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.rbFilterEmpty.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(112)))), ((int)(((byte)(67)))));
            this.rbFilterEmpty.Location = new System.Drawing.Point(10, 100);
            this.rbFilterEmpty.Name = "rbFilterEmpty";
            this.rbFilterEmpty.Size = new System.Drawing.Size(258, 30);
            this.rbFilterEmpty.TabIndex = 3;
            this.rbFilterEmpty.Text = "Alarmsız Arızalar";
            this.rbFilterEmpty.UseVisualStyleBackColor = true;
            this.rbFilterEmpty.CheckedChanged += new System.EventHandler(this.rbFilter_CheckedChanged);
            // 
            // rbFilterAlarmed
            // 
            this.rbFilterAlarmed.Dock = System.Windows.Forms.DockStyle.Top;
            this.rbFilterAlarmed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbFilterAlarmed.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.rbFilterAlarmed.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(74)))), ((int)(((byte)(74)))));
            this.rbFilterAlarmed.Location = new System.Drawing.Point(10, 70);
            this.rbFilterAlarmed.Name = "rbFilterAlarmed";
            this.rbFilterAlarmed.Size = new System.Drawing.Size(258, 30);
            this.rbFilterAlarmed.TabIndex = 2;
            this.rbFilterAlarmed.Text = "Alarmlı Arızalar";
            this.rbFilterAlarmed.UseVisualStyleBackColor = true;
            this.rbFilterAlarmed.CheckedChanged += new System.EventHandler(this.rbFilter_CheckedChanged);
            // 
            // rbFilterAll
            // 
            this.rbFilterAll.Checked = true;
            this.rbFilterAll.Dock = System.Windows.Forms.DockStyle.Top;
            this.rbFilterAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbFilterAll.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.rbFilterAll.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.rbFilterAll.Location = new System.Drawing.Point(10, 40);
            this.rbFilterAll.Name = "rbFilterAll";
            this.rbFilterAll.Size = new System.Drawing.Size(258, 30);
            this.rbFilterAll.TabIndex = 1;
            this.rbFilterAll.TabStop = true;
            this.rbFilterAll.Text = "Tüm Arızalar / Duruşlar";
            this.rbFilterAll.UseVisualStyleBackColor = true;
            this.rbFilterAll.CheckedChanged += new System.EventHandler(this.rbFilter_CheckedChanged);
            // 
            // lblFiltersTitle
            // 
            this.lblFiltersTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFiltersTitle.Font = new System.Drawing.Font("Segoe UI Black", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblFiltersTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.lblFiltersTitle.Location = new System.Drawing.Point(10, 5);
            this.lblFiltersTitle.Name = "lblFiltersTitle";
            this.lblFiltersTitle.Size = new System.Drawing.Size(258, 35);
            this.lblFiltersTitle.TabIndex = 0;
            this.lblFiltersTitle.Text = "DURUM FİLTRELERİ";
            this.lblFiltersTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tpDowntimes);
            this.tabMain.Controls.Add(this.tpCycles);
            this.tabMain.Controls.Add(this.tpRawLogs);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabMain.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.tabMain.ItemSize = new System.Drawing.Size(180, 32);
            this.tabMain.Location = new System.Drawing.Point(0, 0);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(896, 678);
            this.tabMain.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabMain.TabIndex = 0;
            this.tabMain.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.tabMain_DrawItem);
            // 
            // tpDowntimes
            // 
            this.tpDowntimes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.tpDowntimes.Location = new System.Drawing.Point(4, 36);
            this.tpDowntimes.Name = "tpDowntimes";
            this.tpDowntimes.Padding = new System.Windows.Forms.Padding(10);
            this.tpDowntimes.Size = new System.Drawing.Size(888, 638);
            this.tpDowntimes.TabIndex = 0;
            this.tpDowntimes.Text = "Down Conditions";
            // 
            // tpCycles
            // 
            this.tpCycles.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.tpCycles.Location = new System.Drawing.Point(4, 36);
            this.tpCycles.Name = "tpCycles";
            this.tpCycles.Padding = new System.Windows.Forms.Padding(10);
            this.tpCycles.Size = new System.Drawing.Size(888, 638);
            this.tpCycles.TabIndex = 1;
            this.tpCycles.Text = "Üretim Çevrimleri";
            // 
            // tpRawLogs
            // 
            this.tpRawLogs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.tpRawLogs.Location = new System.Drawing.Point(4, 36);
            this.tpRawLogs.Name = "tpRawLogs";
            this.tpRawLogs.Padding = new System.Windows.Forms.Padding(10);
            this.tpRawLogs.Size = new System.Drawing.Size(888, 638);
            this.tpRawLogs.TabIndex = 2;
            this.tpRawLogs.Text = "Ham Veri";

            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(31)))), ((int)(((byte)(31)))));
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus});
            this.statusStrip.Location = new System.Drawing.Point(0, 778);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1200, 22);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 2;
            this.statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(262, 17);
            this.lblStatus.Text = "Hazır. Lütfen bir CSV dosyası yükleyin.";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(12)))), ((int)(((byte)(12)))), ((int)(((byte)(12)))));
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.statusStrip);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DownTracker - Nihai Üretim ve Duruş Analiz Motoru";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.pnlTop.ResumeLayout(false);
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlLeft.ResumeLayout(false);
            this.pnlFilters.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            this.tpDowntimes.ResumeLayout(false);
            this.tpCycles.ResumeLayout(false);
            this.tpRawLogs.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblDragDrop;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.TreeView tvStations;
        private System.Windows.Forms.Label lblStationsTitle;
        private System.Windows.Forms.Panel pnlFilters;
        private System.Windows.Forms.RadioButton rbFilterEmpty;
        private System.Windows.Forms.RadioButton rbFilterAlarmed;
        private System.Windows.Forms.RadioButton rbFilterAll;
        private System.Windows.Forms.Label lblFiltersTitle;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tpDowntimes;
        private System.Windows.Forms.TabPage tpCycles;
        private System.Windows.Forms.TabPage tpRawLogs;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}
