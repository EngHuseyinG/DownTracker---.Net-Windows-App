using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using DownTracker.Models;
using DownTracker.ViewModels;

namespace DownTracker.Views
{
    public class FaultyImportView : UserControl
    {
        private readonly MainViewModel _mainViewModel;
        private readonly FaultyImportViewModel _viewModel;

        private Panel pnlOldExcelDrag;
        private Panel pnlGrafanaCsvDrag;
        private DataGridView dgvFaultyInputPreview;
        private DataGridView dgvGrafanaFaultyPreview;
        private DataGridView dgvFaultyOutputPreview;
        private Label lblOldExcelDrag;
        private Label lblGrafanaCsvDrag;

        public FaultyImportViewModel ViewModel => _viewModel;

        public FaultyImportView(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _viewModel = new FaultyImportViewModel(mainViewModel);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Dock = DockStyle.Fill;

            var pnlFaultyContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(20),
                AutoScroll = true
            };

            // 1️⃣ Drag/Drop Containers Layout
            var pnlDragContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 300,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 20, 0, 15)
            };
            pnlDragContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlDragContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            pnlOldExcelDrag = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(1),
                AllowDrop = true,
                Margin = new Padding(0, 0, 10, 0)
            };
            pnlOldExcelDrag.Resize += (s, e) => pnlOldExcelDrag.Invalidate();
            pnlOldExcelDrag.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(0, 229, 255), 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlOldExcelDrag.Width - 1, pnlOldExcelDrag.Height - 1);
                }
            };

            lblOldExcelDrag = new Label
            {
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Text = "📩 1.INPUT: P360 SİSTEMİNE YÜKLENEN ESKİ EXCELİ BURAYA SÜRÜKLEYİN (.xlsx)",
                TextAlign = ContentAlignment.MiddleCenter,
                AllowDrop = true
            };
            pnlOldExcelDrag.Controls.Add(lblOldExcelDrag);

            pnlGrafanaCsvDrag = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(1),
                AllowDrop = true,
                Margin = new Padding(10, 0, 0, 0)
            };
            pnlGrafanaCsvDrag.Resize += (s, e) => pnlGrafanaCsvDrag.Invalidate();
            pnlGrafanaCsvDrag.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(255, 74, 74), 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlGrafanaCsvDrag.Width - 1, pnlGrafanaCsvDrag.Height - 1);
                }
            };

            lblGrafanaCsvDrag = new Label
            {
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 74, 74),
                Text = "📩 2.INPUT: GRAFANA HATALI ALARMLAR ÇIKTISINI BURAYA SÜRÜKLEYİN (.csv)",
                TextAlign = ContentAlignment.MiddleCenter,
                AllowDrop = true
            };
            pnlGrafanaCsvDrag.Controls.Add(lblGrafanaCsvDrag);

            pnlDragContainer.Controls.Add(pnlOldExcelDrag, 0, 0);
            pnlDragContainer.Controls.Add(pnlGrafanaCsvDrag, 1, 0);

            // Drag Drop Events for Old Excel Drag
            pnlOldExcelDrag.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            pnlOldExcelDrag.DragDrop += async (s, e) => { await HandleOldExcelDrop(e); };
            lblOldExcelDrag.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            lblOldExcelDrag.DragDrop += async (s, e) => { await HandleOldExcelDrop(e); };
            lblOldExcelDrag.Click += async (s, e) => { await SelectAndLoadOldExcel(); };

            // Drag Drop Events for Grafana CSV Drag
            pnlGrafanaCsvDrag.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            pnlGrafanaCsvDrag.DragDrop += async (s, e) => { await HandleGrafanaDrop(e); };
            lblGrafanaCsvDrag.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            lblGrafanaCsvDrag.DragDrop += async (s, e) => { await HandleGrafanaDrop(e); };
            lblGrafanaCsvDrag.Click += async (s, e) => { await SelectAndLoadGrafanaCsv(); };

            // 1️⃣ P360 SİSTEMİNE IMPORT EDİLEN ALARM LİSTESİ ÖRNEĞİ (dgvFaultyInputPreview)
            var lblFaultyInputHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255), // Canlı Turkuaz
                Text = "📩 1. INPUT: P360 SİSTEMİNE IMPORT EDİLEN ESKİ ALARM LİSTESİ ÖRNEĞİ",
                TextAlign = ContentAlignment.MiddleLeft
            };

            dgvFaultyInputPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            ApplyGridTheme(dgvFaultyInputPreview);
            dgvFaultyInputPreview.ColumnHeadersHeight = 32;
            dgvFaultyInputPreview.RowTemplate.Height = 28;

            string[] cols = { "FULLTAGNAME", "DEVICENAME", "COMMENT", "ParentCategory", "Category", "Code" };
            foreach (var col in cols)
            {
                dgvFaultyInputPreview.Columns.Add(col, col);
            }

            dgvFaultyInputPreview.Columns["FULLTAGNAME"]!.Width = 800;
            dgvFaultyInputPreview.Columns["DEVICENAME"]!.Width = 110;
            dgvFaultyInputPreview.Columns["COMMENT"]!.Width = 280;
            dgvFaultyInputPreview.Columns["ParentCategory"]!.Width = 280;
            dgvFaultyInputPreview.Columns["Category"]!.Width = 120;
            dgvFaultyInputPreview.Columns["Code"]!.Width = 75;

            dgvFaultyInputPreview.CellPainting += (s, e) =>
            {
                if (e.Graphics == null) return;
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    Color headerBg = Color.Transparent;
                    Color headerFg = Color.Black;
                    switch (e.ColumnIndex)
                    {
                        case 0: headerBg = Color.FromArgb(183, 222, 232); break;
                        case 1: headerBg = Color.FromArgb(255, 192, 0); break;
                        case 2: headerBg = Color.FromArgb(252, 228, 214); break;
                        case 3: headerBg = Color.FromArgb(234, 241, 221); break;
                        case 4: headerBg = Color.FromArgb(234, 241, 221); break;
                        case 5: headerBg = Color.FromArgb(191, 191, 191); break;
                    }
                    e.Graphics.FillRectangle(new SolidBrush(headerBg), e.CellBounds);
                    using (var pen = new Pen(Color.FromArgb(55, 55, 55))) e.Graphics.DrawRectangle(pen, e.CellBounds);
                    using (var font = new Font("Calibri", 12f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(e.Graphics, e.Value?.ToString(), font, e.CellBounds, headerFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    e.Handled = true;
                }
                else if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(26, 26, 26)), e.CellBounds);
                    using (var pen = new Pen(Color.FromArgb(55, 55, 55)))
                    {
                        e.Graphics.DrawRectangle(pen, e.CellBounds.X - 1, e.CellBounds.Y - 1, e.CellBounds.Width, e.CellBounds.Height);
                    }
                    if (e.Value != null)
                    {
                        using (var font = new Font("Calibri", 11f, FontStyle.Regular))
                        {
                            TextRenderer.DrawText(e.Graphics, e.Value.ToString(), font, e.CellBounds, Color.LightGray,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        }
                    }
                    e.Handled = true;
                }
            };

            // Sample input preview values
            dgvFaultyInputPreview.Rows.Add(
                "OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1.PLC1.v710.Underbody.ZONE0.PLC1.Errors.Alarm.DiscreteAlarm1",
                "PLC1",
                "9A Power Alarm F100",
                "EquipmentDownCondition",
                "Elektrik (E)",
                "FC-1019"
            );
            dgvFaultyInputPreview.Rows.Add(
                "OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1.PLC1.v710.Underbody.ZONE0.PLC1.Errors.Alarm.DiscreteAlarm2",
                "PLC1",
                "9A Power Alarm F200",
                "EquipmentDownCondition",
                "Elektrik (E)",
                "FC-1020"
            );
            dgvFaultyInputPreview.Rows.Add(
                "OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1.PLC1.v710.Underbody.ZONE0.PLC1.Errors.Alarm.DiscreteAlarm3",
                "PLC1",
                "9A Power Alarm F300",
                "EquipmentDownCondition",
                "Elektrik (E)",
                "FC-1021"
            );

            var pnlInputWrapper = new Panel
            {
                BackColor = Color.Transparent,
                Height = 170
            };
            pnlInputWrapper.Controls.Add(lblFaultyInputHeader);
            BindCustomScrollbar(dgvFaultyInputPreview, pnlInputWrapper, 135);
            dgvFaultyInputPreview.Margin = new Padding(0, 0, 0, 5);

            // 2️⃣ HATA ALINAN GRAFANA ALARM FORMATI ÖRNEĞİ (dgvGrafanaFaultyPreview)
            var lblGrafanaFaultyHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 74, 74), // Neon Kırmızı
                Text = "📩 2. INPUT: GRAFANA HATALI IMPORT EDİLEN ALARM LİSTESİ",
                TextAlign = ContentAlignment.MiddleLeft
            };

            dgvGrafanaFaultyPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            ApplyGridTheme(dgvGrafanaFaultyPreview);
            dgvGrafanaFaultyPreview.ColumnHeadersHeight = 32;
            dgvGrafanaFaultyPreview.RowTemplate.Height = 28;
            dgvGrafanaFaultyPreview.Columns.Add("SensorName", "SensorName");
            dgvGrafanaFaultyPreview.Columns["SensorName"]!.Width = 800;

            dgvGrafanaFaultyPreview.CellPainting += (s, e) =>
            {
                if (e.Graphics == null) return;
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    Color headerBg = Color.FromArgb(255, 74, 74);
                    Color headerFg = Color.White;
                    e.Graphics.FillRectangle(new SolidBrush(headerBg), e.CellBounds);
                    using (var pen = new Pen(Color.FromArgb(55, 55, 55))) e.Graphics.DrawRectangle(pen, e.CellBounds);
                    using (var font = new Font("Calibri", 12f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(e.Graphics, e.Value?.ToString(), font, e.CellBounds, headerFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    e.Handled = true;
                }
                else if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(26, 26, 26)), e.CellBounds);
                    using (var pen = new Pen(Color.FromArgb(55, 55, 55)))
                    {
                        e.Graphics.DrawRectangle(pen, e.CellBounds.X - 1, e.CellBounds.Y - 1, e.CellBounds.Width, e.CellBounds.Height);
                    }
                    if (e.Value != null)
                    {
                        using (var font = new Font("Calibri", 11f, FontStyle.Regular))
                        {
                            TextRenderer.DrawText(e.Graphics, e.Value.ToString(), font, e.CellBounds, Color.LightGray,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        }
                    }
                    e.Handled = true;
                }
            };
            dgvGrafanaFaultyPreview.Rows.Add("OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1.PLC1.v710.Underbody.ZONE0.PLC1.Errors.Alarm.DiscreteAlarm1");

            var pnlGrafanaWrapper = new Panel
            {
                BackColor = Color.Transparent,
                Height = 135,
                Padding = new Padding(0, 15, 0, 0)
            };
            pnlGrafanaWrapper.Controls.Add(lblGrafanaFaultyHeader);
            BindCustomScrollbar(dgvGrafanaFaultyPreview, pnlGrafanaWrapper, 75);
            dgvGrafanaFaultyPreview.Margin = new Padding(0, 0, 0, 5);

            // 3️⃣ P360'A TEKRAR IMPORT EDİLECEK ALARM VERİ ÖRNEĞİ (dgvFaultyOutputPreview)
            var lblFaultyOutputHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 145, 0), // Parlak Turuncu
                Text = "📤 3. OUTPUT: P360'A TEKRAR IMPORT EDİLECEK  ALARM LİSTESİ",
                TextAlign = ContentAlignment.MiddleLeft
            };

            dgvFaultyOutputPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            ApplyGridTheme(dgvFaultyOutputPreview);
            dgvFaultyOutputPreview.ColumnHeadersHeight = 32;
            dgvFaultyOutputPreview.RowTemplate.Height = 28;

            foreach (var col in cols)
            {
                dgvFaultyOutputPreview.Columns.Add(col, col);
            }

            dgvFaultyOutputPreview.Columns["FULLTAGNAME"]!.Width = 800;
            dgvFaultyOutputPreview.Columns["DEVICENAME"]!.Width = 110;
            dgvFaultyOutputPreview.Columns["COMMENT"]!.Width = 280;
            dgvFaultyOutputPreview.Columns["ParentCategory"]!.Width = 280;
            dgvFaultyOutputPreview.Columns["Category"]!.Width = 120;
            dgvFaultyOutputPreview.Columns["Code"]!.Width = 75;

            dgvFaultyOutputPreview.CellPainting += (s, e) =>
            {
                if (e.Graphics == null) return;
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    Color headerBg = Color.Transparent;
                    Color headerFg = Color.Black;
                    switch (e.ColumnIndex)
                    {
                        case 0: headerBg = Color.FromArgb(183, 222, 232); break;
                        case 1: headerBg = Color.FromArgb(255, 192, 0); break;
                        case 2: headerBg = Color.FromArgb(252, 228, 214); break;
                        case 3: headerBg = Color.FromArgb(234, 241, 221); break;
                        case 4: headerBg = Color.FromArgb(234, 241, 221); break;
                        case 5: headerBg = Color.FromArgb(191, 191, 191); break;
                    }
                    e.Graphics.FillRectangle(new SolidBrush(headerBg), e.CellBounds);
                    using (var pen = new Pen(Color.FromArgb(55, 55, 55))) e.Graphics.DrawRectangle(pen, e.CellBounds);
                    using (var font = new Font("Calibri", 12f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(e.Graphics, e.Value?.ToString(), font, e.CellBounds, headerFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    e.Handled = true;
                }
                else if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(26, 26, 26)), e.CellBounds);
                    using (var pen = new Pen(Color.FromArgb(55, 55, 55)))
                    {
                        e.Graphics.DrawRectangle(pen, e.CellBounds.X - 1, e.CellBounds.Y - 1, e.CellBounds.Width, e.CellBounds.Height);
                    }
                    if (e.Value != null)
                    {
                        using (var font = new Font("Calibri", 11f, FontStyle.Regular))
                        {
                            TextRenderer.DrawText(e.Graphics, e.Value.ToString(), font, e.CellBounds, Color.LightGray,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        }
                    }
                    e.Handled = true;
                }
            };

            dgvFaultyOutputPreview.Rows.Add(
                "OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1.PLC1.v710.Underbody.ZONE0.PLC1.Errors.Alarm.DiscreteAlarm1",
                "PLC1",
                "9A Power Alarm F100",
                "EquipmentDownCondition",
                "Elektrik (E)",
                "FC-1019"
            );

            var pnlOutputWrapper = new Panel
            {
                BackColor = Color.Transparent,
                Height = 115
            };
            pnlOutputWrapper.Controls.Add(lblFaultyOutputHeader);
            BindCustomScrollbar(dgvFaultyOutputPreview, pnlOutputWrapper, 75);
            dgvFaultyOutputPreview.Margin = new Padding(0, 0, 0, 5);

            // Add all controls to content panel
            pnlFaultyContent.Controls.Add(pnlOutputWrapper);
            pnlFaultyContent.Controls.Add(pnlGrafanaWrapper);
            pnlFaultyContent.Controls.Add(pnlInputWrapper);
            pnlFaultyContent.Controls.Add(pnlDragContainer);

            // Z-Order layout configuration
            pnlDragContainer.Dock = DockStyle.Top;
            pnlInputWrapper.Dock = DockStyle.Top;
            pnlGrafanaWrapper.Dock = DockStyle.Top;
            pnlOutputWrapper.Dock = DockStyle.Top;

            pnlDragContainer.SendToBack();
            pnlOutputWrapper.SendToBack();
            pnlGrafanaWrapper.SendToBack();
            pnlInputWrapper.SendToBack();

            this.Controls.Add(pnlFaultyContent);
        }

        private void BindCustomScrollbar(DataGridView dgv, Panel containerPanel, int gridHeight)
        {
            dgv.ScrollBars = ScrollBars.None;
            dgv.Dock = DockStyle.Top;
            dgv.Height = gridHeight;

            containerPanel.Dock = DockStyle.Top;

            var hScroll = new HScrollBar
            {
                Dock = DockStyle.Bottom,
                Height = 10,
                Minimum = 0,
                Value = 0
            };

            containerPanel.Controls.Add(hScroll);
            containerPanel.Controls.Add(dgv);
            dgv.BringToFront();

            void SyncScrollbar()
            {
                int totalWidth = 0;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Visible) totalWidth += col.Width;
                }
                int clientWidth = dgv.ClientSize.Width;
                int maxScroll = totalWidth - clientWidth;

                if (maxScroll > 0)
                {
                    hScroll.Visible = true;
                    hScroll.Minimum = 0;
                    hScroll.LargeChange = clientWidth;
                    hScroll.Maximum = totalWidth;
                    hScroll.Value = Math.Max(0, Math.Min(dgv.HorizontalScrollingOffset, totalWidth - clientWidth));
                }
                else
                {
                    hScroll.Visible = false;
                }
            }

            dgv.Scroll += (s, e) =>
            {
                if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
                {
                    int val = e.NewValue;
                    if (val >= hScroll.Minimum && val <= hScroll.Maximum - hScroll.LargeChange + 1)
                    {
                        hScroll.Value = val;
                    }
                }
            };

            hScroll.Scroll += (s, e) =>
            {
                try
                {
                    dgv.HorizontalScrollingOffset = e.NewValue;
                }
                catch { }
            };

            dgv.Resize += (s, e) => SyncScrollbar();
            dgv.Layout += (s, e) => SyncScrollbar();
            dgv.DataSourceChanged += (s, e) => SyncScrollbar();
            this.Load += (s, e) => SyncScrollbar();

            dgv.Paint += (s, e) =>
            {
                if (hScroll.Visible && hScroll.Value != dgv.HorizontalScrollingOffset)
                {
                    int val = dgv.HorizontalScrollingOffset;
                    if (val >= hScroll.Minimum && val <= hScroll.Maximum - hScroll.LargeChange + 1)
                    {
                        hScroll.Value = val;
                    }
                }
            };
        }

        private void ApplyGridTheme(DataGridView dgv)
        {
            dgv.BackgroundColor = Color.FromArgb(26, 26, 26);
            dgv.GridColor = Color.FromArgb(55, 55, 55);
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            dgv.DefaultCellStyle.BackColor = Color.FromArgb(26, 26, 26);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 220);
            dgv.DefaultCellStyle.Font = new Font("Calibri", 9F, FontStyle.Regular);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 50, 50);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(200, 200, 200);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;

            dgv.RowTemplate.Height = 25;
            dgv.ColumnHeadersHeight = 28;
            dgv.ScrollBars = ScrollBars.Horizontal;
        }

        private async Task HandleOldExcelDrop(DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                await LoadOldExcel(files[0]);
            }
        }

        private async Task SelectAndLoadOldExcel()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel ve CSV Dosyaları (*.xlsx;*.csv)|*.xlsx;*.csv";
                openFileDialog.Title = "Eski Excel Dosyasını Seçin";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    await LoadOldExcel(openFileDialog.FileName);
                }
            }
        }

        private async Task LoadOldExcel(string filePath)
        {
            try
            {
                SetParentLoading(true, "Eski P360 Excel dosyası okunuyor...");
                _viewModel.StatusText = "Eski P360 Excel dosyası okunuyor...";
                lblOldExcelDrag.Text = $"📩 1.INPUT: Okunuyor: {Path.GetFileName(filePath)}";

                var rows = await ParseP360ExcelAsync(filePath);
                _viewModel.OldP360Rows = rows;

                lblOldExcelDrag.Text = $"📩 1.INPUT: Seçilen Eski Excel: {Path.GetFileName(filePath)}\n({_viewModel.OldP360Rows.Count} satır yüklendi)";
                _viewModel.StatusText = $"Eski P360 Excel dosyasından {_viewModel.OldP360Rows.Count} satır yüklendi.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dosya okunurken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblOldExcelDrag.Text = "📩 1.INPUT: P360 SİSTEMİNE YÜKLENEN ESKİ EXCELİ BURAYA SÜRÜKLEYİN (.xlsx)";
            }
            finally
            {
                SetParentLoading(false);
            }
        }

        private async Task HandleGrafanaDrop(DragEventArgs e)
        {
            if (_viewModel.OldP360Rows == null || _viewModel.OldP360Rows.Count == 0)
            {
                MessageBox.Show("Lütfen önce eski P360 Excel dosyasını yükleyin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                lblGrafanaCsvDrag.Text = $"📩 2.INPUT: Seçilen Grafana CSV: {Path.GetFileName(files[0])}";
                await ExtractAndSaveFaultyImportAsync(files[0]);
            }
        }

        private async Task SelectAndLoadGrafanaCsv()
        {
            if (_viewModel.OldP360Rows == null || _viewModel.OldP360Rows.Count == 0)
            {
                MessageBox.Show("Lütfen önce eski P360 Excel dosyasını yükleyin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV Dosyaları (*.csv)|*.csv";
                openFileDialog.Title = "Grafana CSV Dosyasını Seçin";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    lblGrafanaCsvDrag.Text = $"📩 2.INPUT: Seçilen Grafana CSV: {Path.GetFileName(openFileDialog.FileName)}";
                    await ExtractAndSaveFaultyImportAsync(openFileDialog.FileName);
                }
            }
        }

        private async Task<List<string[]>> ParseP360ExcelAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var rows = new List<string[]>();
                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == ".xlsx")
                {
                    var sharedStrings = new List<string>();
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        var sstEntry = archive.GetEntry("xl/sharedStrings.xml");
                        if (sstEntry != null)
                        {
                            using (var stream = sstEntry.Open())
                            {
                                XDocument sstDoc = XDocument.Load(stream);
                                XNamespace ns = sstDoc.Root.Name.Namespace;
                                foreach (var si in sstDoc.Root.Elements(ns + "si"))
                                {
                                    var textNodes = si.Descendants(ns + "t").Select(tNode => tNode.Value);
                                    sharedStrings.Add(string.Concat(textNodes));
                                }
                            }
                        }

                        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                        if (sheetEntry != null)
                        {
                            using (var stream = sheetEntry.Open())
                            {
                                XDocument sheetDoc = XDocument.Load(stream);
                                XNamespace ns = sheetDoc.Root.Name.Namespace;
                                var sheetData = sheetDoc.Root.Element(ns + "sheetData");
                                if (sheetData != null)
                                {
                                    bool isFirst = true;
                                    foreach (var row in sheetData.Elements(ns + "row"))
                                    {
                                        if (isFirst) { isFirst = false; continue; } // Skip header

                                        string[] rowValues = new string[6];
                                        for (int j = 0; j < 6; j++) rowValues[j] = "";

                                        foreach (var cell in row.Elements(ns + "c"))
                                        {
                                            string cellRef = (string)cell.Attribute("r") ?? "";
                                            if (string.IsNullOrEmpty(cellRef)) continue;

                                            // Convert column letter to index
                                            int i = 0;
                                            while (i < cellRef.Length && char.IsLetter(cellRef[i])) i++;
                                            string colLetters = cellRef.Substring(0, i).ToUpperInvariant();
                                            int colIdx = 0;
                                            foreach (char c in colLetters) colIdx = colIdx * 26 + (c - 'A' + 1);
                                            colIdx--; // 0-based

                                            if (colIdx >= 0 && colIdx < 6)
                                            {
                                                // Get cell value
                                                string t = (string)cell.Attribute("t");
                                                string rawValue = cell.Element(ns + "v")?.Value ?? "";
                                                string val = "";
                                                if (t == "s")
                                                {
                                                    if (int.TryParse(rawValue, out int idx) && idx >= 0 && idx < sharedStrings.Count)
                                                        val = sharedStrings[idx];
                                                }
                                                else if (t == "inlineStr")
                                                {
                                                    val = cell.Element(ns + "is")?.Element(ns + "t")?.Value ?? "";
                                                }
                                                else
                                                {
                                                    val = rawValue;
                                                }
                                                rowValues[colIdx] = val.Trim('"', ' ', '\r', '\n', '\t');
                                            }
                                        }
                                        if (rowValues.Any(val => !string.IsNullOrEmpty(val)))
                                        {
                                            rows.Add(rowValues);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else // .csv
                {
                    Encoding encoding = Encoding.UTF8;
                    if (File.Exists(filePath))
                    {
                        byte[] bom = new byte[4];
                        using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            file.Read(bom, 0, 4);
                        }
                        if (bom[0] == 0xff && bom[1] == 0xfe) encoding = Encoding.Unicode;
                        else if (bom[0] == 0xfe && bom[1] == 0xff) encoding = Encoding.BigEndianUnicode;
                        else if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) encoding = Encoding.UTF8;
                    }

                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs, encoding))
                    {
                        string line;
                        bool isFirst = true;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            if (isFirst) { isFirst = false; continue; } // Skip header

                            string[] parts = line.Split(';');
                            if (parts.Length < 6) parts = line.Split(',');

                            string[] rowValues = new string[6];
                            for (int j = 0; j < 6; j++)
                            {
                                rowValues[j] = j < parts.Length ? parts[j].Trim('"', ' ', '\r', '\n', '\t') : "";
                            }
                            if (rowValues.Any(val => !string.IsNullOrEmpty(val)))
                            {
                                rows.Add(rowValues);
                            }
                        }
                    }
                }
                return rows;
            });
        }

        private async Task<HashSet<string>> ParseGrafanaCsvAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Encoding encoding = Encoding.UTF8;
                if (File.Exists(filePath))
                {
                    byte[] bom = new byte[4];
                    using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        file.Read(bom, 0, 4);
                    }
                    if (bom[0] == 0xff && bom[1] == 0xfe) encoding = Encoding.Unicode;
                    else if (bom[0] == 0xfe && bom[1] == 0xff) encoding = Encoding.BigEndianUnicode;
                    else if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) encoding = Encoding.UTF8;
                }

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, encoding))
                {
                    string line;
                    bool isFirst = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (isFirst) { isFirst = false; continue; } // Skip header

                        string[] parts = line.Split(';');
                        if (parts.Length < 1) parts = line.Split(',');

                        string tag = parts[0].Trim('"', ' ', '\r', '\n', '\t');
                        if (!string.IsNullOrEmpty(tag))
                        {
                            tags.Add(tag);
                        }
                    }
                }
                return tags;
            });
        }

        private async Task ExtractAndSaveFaultyImportAsync(string grafanaFilePath)
        {
            try
            {
                SetParentLoading(true, "Grafana CSV okunuyor...");
                _viewModel.StatusText = "Grafana CSV okunuyor...";

                var grafanaTags = await ParseGrafanaCsvAsync(grafanaFilePath);

                _viewModel.StatusText = "Eşleşen satırlar ayıklanıyor...";

                var matchedRows = await Task.Run(() =>
                {
                    var list = new List<string[]>();
                    foreach (var row in _viewModel.OldP360Rows)
                    {
                        if (row.Length > 0 && !string.IsNullOrEmpty(row[0]))
                        {
                            if (grafanaTags.Contains(row[0]))
                            {
                                list.Add(row);
                            }
                        }
                    }
                    return list;
                });

                if (matchedRows.Count == 0)
                {
                    MessageBox.Show("Eşleşen herhangi bir alarm satırı bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _viewModel.StatusText = $"Eşleşen {matchedRows.Count} satır bulundu.";

                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.FileName = "P360_HataliImport_KurtarmaPaketi.xlsx";
                    saveFileDialog.Filter = "Excel Çalışma Kitabı (*.xlsx)|*.xlsx";
                    saveFileDialog.Title = "Kurtarılmış Alarm Listesini Kaydet";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string targetPath = saveFileDialog.FileName;
                        _viewModel.StatusText = "Excel dosyası kaydediliyor...";

                        var records = matchedRows.Select(row => new ImportedAlarmRecord
                        {
                            FullTagName = row[0],
                            DeviceName = row[1],
                            Comment = row[2],
                            ParentCategory = row[3],
                            Category = row[4],
                            Code = row[5]
                        }).ToList();

                        await Task.Run(() =>
                        {
                            ParsingEngine.SaveToExcel(targetPath, records);
                        });

                        string successMsg = $"Kurtarma paketi başarıyla oluşturuldu: {records.Count} satır kaydedildi.";
                        _viewModel.StatusText = successMsg;
                        MessageBox.Show(successMsg, "İşlem Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Reset states and GUI labels on success
                        _viewModel.OldP360Rows.Clear();
                        if (lblOldExcelDrag != null)
                        {
                            lblOldExcelDrag.Text = "📩 1.INPUT: P360 SİSTEMİNE YÜKLENEN ESKİ EXCELİ BURAYA SÜRÜKLEYİN (.xlsx)";
                        }
                        if (lblGrafanaCsvDrag != null)
                        {
                            lblGrafanaCsvDrag.Text = "📩 2.INPUT: GRAFANA HATALI ALARMLAR ÇIKTISINI BURAYA SÜRÜKLEYİN (.csv)";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İşlem esnasında bir hata oluştu:\n\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _viewModel.StatusText = "İşlem başarısız oldu.";
            }
            finally
            {
                SetParentLoading(false);
            }
        }

        private void SetParentLoading(bool visible, string text = "")
        {
            if (this.ParentForm is MainForm mainForm)
            {
                mainForm.SetLoadingOverlay(visible, text);
            }
        }
    }
}
