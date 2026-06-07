using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DownTracker.Models;
using DownTracker.ViewModels;

namespace DownTracker.Views
{
    public class ImportWizardView : UserControl
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ImportWizardViewModel _viewModel;

        private Panel pnlImportDragDrop;
        private Label lblImportDragDrop;

        public ImportWizardViewModel ViewModel => _viewModel;

        public ImportWizardView(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _viewModel = new ImportWizardViewModel(mainViewModel);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Dock = DockStyle.Fill;

            var pnlImportContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(20)
            };

            pnlImportDragDrop = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 280,
                BackColor = Color.FromArgb(26, 26, 26),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(1),
                AllowDrop = true
            };
            pnlImportDragDrop.Resize += (s, e) => pnlImportDragDrop.Invalidate();
            pnlImportDragDrop.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(0, 229, 255), 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlImportDragDrop.Width - 1, pnlImportDragDrop.Height - 1);
                }
            };

            lblImportDragDrop = new Label
            {
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Text = "📩 1. INPUT: IIoT ENGINEER ALARM IMPORT EXCELİNİ P360 IMPORTUNA HAZIR HALE GETİRMEK İÇİN DOSYAYI BURAYA SÜRÜKLEYİN VEYA TIKLAYIN (.xlsx)",
                TextAlign = ContentAlignment.MiddleCenter,
                AllowDrop = true
            };

            // Drag and Drop Events
            pnlImportDragDrop.DragEnter += PnlImportDragDrop_DragEnter;
            pnlImportDragDrop.DragLeave += PnlImportDragDrop_DragLeave;
            pnlImportDragDrop.DragDrop += PnlImportDragDrop_DragDrop;

            lblImportDragDrop.DragEnter += PnlImportDragDrop_DragEnter;
            lblImportDragDrop.DragLeave += PnlImportDragDrop_DragLeave;
            lblImportDragDrop.DragDrop += PnlImportDragDrop_DragDrop;
            lblImportDragDrop.Click += LblImportDragDrop_Click;

            pnlImportDragDrop.Controls.Add(lblImportDragDrop);

            // Preview Container (pnlPreviewContainer)
            var pnlPreviewContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 360,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 15)
            };

            var tlpPreview = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            tlpPreview.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpPreview.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Input Preview Row
            var pnlInputRow = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var pnlInputHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.Transparent
            };

            var lblInputHeader = new Label
            {
                Dock = DockStyle.Left,
                Width = 550,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Text = "📩 1.INPUT: BEKLENEN INPUT EXCEL FORMATI VE ÖRNEK VERİ HATTI",
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnDownloadTemplate = new Button
            {
                Size = new Size(200, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.FromArgb(0, 229, 255),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "📥 Örnek Input Şablonu İndir",
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnDownloadTemplate.FlatAppearance.BorderColor = Color.FromArgb(0, 229, 255);
            btnDownloadTemplate.FlatAppearance.BorderSize = 1;

            // Hover effects
            btnDownloadTemplate.MouseEnter += (s, e) => { btnDownloadTemplate.BackColor = Color.FromArgb(0, 229, 255); btnDownloadTemplate.ForeColor = Color.Black; };
            btnDownloadTemplate.MouseLeave += (s, e) => { btnDownloadTemplate.BackColor = Color.FromArgb(25, 25, 25); btnDownloadTemplate.ForeColor = Color.FromArgb(0, 229, 255); };
            btnDownloadTemplate.Click += BtnDownloadTemplate_Click;

            pnlInputHeader.Controls.Add(btnDownloadTemplate);
            pnlInputHeader.Controls.Add(lblInputHeader);

            var dgvInputPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            ApplyGridTheme(dgvInputPreview);
            dgvInputPreview.ColumnHeadersHeight = 32;
            dgvInputPreview.RowTemplate.Height = 28;

            string[] headers = new string[] {
                "CHANNEL NAME", "DEVICE NAME", "LINE NAME (LVL1)", "LINE NAME (LVL2)", "LINE NAME (LVL3)", "STATION NAME",
                "Res", "Res", "Tag Name", "PLC Adress", "Data Type", "A/D", "RO//R/W", "SCAN RATE",
                "COMMENT", "Category Name", "SubCategory Name", "FULL TAG NAME"
            };

            dgvInputPreview.Columns.Add("CHANNEL", headers[0]);
            dgvInputPreview.Columns.Add("DEVICE", headers[1]);
            dgvInputPreview.Columns.Add("LVL1", headers[2]);
            dgvInputPreview.Columns.Add("LVL2", headers[3]);
            dgvInputPreview.Columns.Add("LVL3", headers[4]);
            dgvInputPreview.Columns.Add("STATION", headers[5]);
            dgvInputPreview.Columns.Add("RES1", headers[6]);
            dgvInputPreview.Columns.Add("RES2", headers[7]);
            dgvInputPreview.Columns.Add("TAG", headers[8]);
            dgvInputPreview.Columns.Add("PLC", headers[9]);
            dgvInputPreview.Columns.Add("TYPE", headers[10]);
            dgvInputPreview.Columns.Add("AD", headers[11]);
            dgvInputPreview.Columns.Add("RW", headers[12]);
            dgvInputPreview.Columns.Add("SCAN", headers[13]);
            dgvInputPreview.Columns.Add("COMMENT", headers[14]);
            dgvInputPreview.Columns.Add("PCAT", headers[15]);
            dgvInputPreview.Columns.Add("CAT", headers[16]);
            dgvInputPreview.Columns.Add("FULLTAG", headers[17]);

            dgvInputPreview.Columns["CHANNEL"].Width = 420;
            dgvInputPreview.Columns["DEVICE"].Width = 120;
            dgvInputPreview.Columns["LVL1"].Width = 160;
            dgvInputPreview.Columns["LVL2"].Width = 160;
            dgvInputPreview.Columns["LVL3"].Width = 160;
            dgvInputPreview.Columns["STATION"].Width = 140;
            dgvInputPreview.Columns["RES1"].Width = 75;
            dgvInputPreview.Columns["RES2"].Width = 75;
            dgvInputPreview.Columns["TAG"].Width = 120;
            dgvInputPreview.Columns["PLC"].Width = 140;
            dgvInputPreview.Columns["TYPE"].Width = 110;
            dgvInputPreview.Columns["AD"].Width = 85;
            dgvInputPreview.Columns["RW"].Width = 110;
            dgvInputPreview.Columns["SCAN"].Width = 120;
            dgvInputPreview.Columns["COMMENT"].Width = 350;
            dgvInputPreview.Columns["PCAT"].Width = 160;
            dgvInputPreview.Columns["CAT"].Width = 160;
            dgvInputPreview.Columns["FULLTAG"].Width = 480;

            // Custom painting for headers/rows
            dgvInputPreview.CellPainting += (s, e) =>
            {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    Color headerBg = Color.FromArgb(255, 165, 0);
                    switch (e.ColumnIndex)
                    {
                        case 0: case 1: headerBg = Color.FromArgb(255, 192, 0); break;
                        case 2: case 3: case 4: headerBg = Color.FromArgb(146, 208, 80); break;
                        case 5: headerBg = Color.FromArgb(0, 176, 240); break;
                        case 6: case 7: headerBg = Color.FromArgb(191, 191, 191); break;
                        case 8: case 9: headerBg = Color.FromArgb(255, 255, 0); break;
                        case 10: headerBg = Color.FromArgb(217, 150, 148); break;
                        case 11: case 12: headerBg = Color.FromArgb(217, 217, 217); break;
                        case 13: headerBg = Color.FromArgb(146, 205, 220); break;
                        case 14: headerBg = Color.FromArgb(252, 228, 214); break;
                        case 15: case 16: headerBg = Color.FromArgb(234, 241, 221); break;
                        case 17: headerBg = Color.FromArgb(183, 222, 232); break;
                    }

                    e.Graphics.FillRectangle(new SolidBrush(headerBg), e.CellBounds);
                    using (var pen = new Pen(Color.FromArgb(55, 55, 55)))
                    {
                        e.Graphics.DrawRectangle(pen, e.CellBounds.X - 1, e.CellBounds.Y - 1, e.CellBounds.Width, e.CellBounds.Height);
                    }
                    if (e.Value != null)
                    {
                        using (var font = new Font("Calibri", 12f, FontStyle.Bold))
                        {
                            TextRenderer.DrawText(e.Graphics, e.Value.ToString(), font, e.CellBounds, Color.Black,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                    e.Handled = true;
                }
                else if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(18, 18, 18)), e.CellBounds);
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

            dgvInputPreview.Rows.Clear();
            dgvInputPreview.Rows.Add(
                "OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1", "PLC1", "v710", "Underbody", "ZONE0", "PLC1", "Errors", "Alarm", "DiscreteAlarm1", "DB2100.DBX270.0", "Boolean", "1", "RO", "1000", "9A Power Alarm F100", "EquipmentDownCondition", "Elektrik (E)", "OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1.PLC1.v710.Underbody.ZONE0.PLC1.Errors.Alarm.DiscreteAlarm1"
            );

            var pnlInputGridWrapper = new Panel
            {
                BackColor = Color.Transparent
            };
            pnlInputRow.Controls.Add(pnlInputGridWrapper);
            pnlInputRow.Controls.Add(pnlInputHeader);
            pnlInputHeader.SendToBack();
            BindCustomScrollbar(dgvInputPreview, pnlInputGridWrapper, 62);

            // Output Preview Row
            var pnlOutputRow = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 16, 0, 0)
            };

            var lblOutputTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 145, 0),
                Text = "📤 2.OUTPUT: ÜRETİLECEK P360 EXCEL FORMATI VE ÖRNEK VERİ HATTI"
            };

            var dgvOutputPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            ApplyGridTheme(dgvOutputPreview);
            dgvOutputPreview.ColumnHeadersHeight = 32;
            dgvOutputPreview.RowTemplate.Height = 28;

            dgvOutputPreview.Columns.Add("FULLTAGNAME", "FULLTAGNAME");
            dgvOutputPreview.Columns.Add("DEVICENAME", "DEVICENAME");
            dgvOutputPreview.Columns.Add("COMMENT", "COMMENT");
            dgvOutputPreview.Columns.Add("ParentCategory", "ParentCategory");
            dgvOutputPreview.Columns.Add("Category", "Category");
            dgvOutputPreview.Columns.Add("Code", "Code");

            dgvOutputPreview.Columns["FULLTAGNAME"].Width = 800;
            dgvOutputPreview.Columns["DEVICENAME"].Width = 115;
            dgvOutputPreview.Columns["COMMENT"].Width = 350;
            dgvOutputPreview.Columns["ParentCategory"].Width = 180;
            dgvOutputPreview.Columns["Category"].Width = 140;
            dgvOutputPreview.Columns["Code"].Width = 75;

            // Custom cell paint
            dgvOutputPreview.CellPainting += (s, e) =>
            {
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
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(18, 18, 18)), e.CellBounds);
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

            dgvOutputPreview.Rows.Clear();
            dgvOutputPreview.Rows.Add(
                "OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1.PLC1.v710.Underbody.ZONE0.PLC1.Errors.Alarm.DiscreteAlarm1",
                "PLC1",
                "9A Power Alarm F100",
                "EquipmentDownCondition",
                "Elektrik (E)",
                "FC-1019"
            );

            var pnlOutputGridWrapper = new Panel
            {
                BackColor = Color.Transparent
            };
            pnlOutputRow.Controls.Add(pnlOutputGridWrapper);
            pnlOutputRow.Controls.Add(lblOutputTitle);
            lblOutputTitle.SendToBack();
            BindCustomScrollbar(dgvOutputPreview, pnlOutputGridWrapper, 62);

            tlpPreview.Controls.Add(pnlInputRow, 0, 0);
            tlpPreview.Controls.Add(pnlOutputRow, 0, 1);
            pnlPreviewContainer.Controls.Add(tlpPreview);

            pnlImportContent.Controls.Add(pnlImportDragDrop);
            pnlImportContent.Controls.Add(pnlPreviewContainer);

            this.Controls.Add(pnlImportContent);
        }

        private void BindCustomScrollbar(DataGridView dgv, Panel containerPanel, int gridHeight)
        {
            dgv.ScrollBars = ScrollBars.None;
            dgv.Dock = DockStyle.Top;
            dgv.Height = gridHeight;

            containerPanel.Dock = DockStyle.Top;
            containerPanel.Height = gridHeight + 10;

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
            
            // Trigger SyncScrollbar after control is loaded
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

            dgv.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
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

        private async void BtnDownloadTemplate_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.FileName = "P360_Beklenen_Input_Sablonu.xlsx";
                saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                saveFileDialog.Title = "Örnek Input Excel Şablonunu Kaydet";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    try
                    {
                        SetParentLoading(true, "Örnek şablon oluşturuluyor...");
                        _viewModel.StatusText = "Örnek şablon oluşturuluyor...";

                        await Task.Run(() =>
                        {
                            ParsingEngine.GenerateInputTemplate(filePath);
                        });

                        string successMsg = $"Örnek şablon başarıyla oluşturuldu: {Path.GetFileName(filePath)}";
                        _viewModel.StatusText = successMsg;
                        MessageBox.Show(successMsg, "Şablon İndirildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Şablon oluşturulurken bir hata oluştu:\n\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _viewModel.StatusText = "Şablon oluşturulamadı.";
                    }
                    finally
                    {
                        SetParentLoading(false);
                    }
                }
            }
        }

        private void PnlImportDragDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                pnlImportDragDrop.BackColor = Color.FromArgb(45, 45, 45);
                lblImportDragDrop.ForeColor = Color.FromArgb(0, 229, 255);
            }
        }

        private void PnlImportDragDrop_DragLeave(object sender, EventArgs e)
        {
            pnlImportDragDrop.BackColor = Color.FromArgb(26, 26, 26);
            lblImportDragDrop.ForeColor = Color.FromArgb(0, 229, 255);
        }

        private void PnlImportDragDrop_DragDrop(object sender, DragEventArgs e)
        {
            pnlImportDragDrop.BackColor = Color.FromArgb(26, 26, 26);
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                _ = ProcessAndSaveFileAsync(files[0]);
            }
        }

        private void LblImportDragDrop_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel ve CSV Dosyaları (*.xlsx;*.csv)|*.xlsx;*.csv|Excel Dosyaları (*.xlsx)|*.xlsx|CSV Dosyaları (*.csv)|*.csv";
                openFileDialog.Title = "Alarm Modifikasyon Dosyasını Seçin";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _ = ProcessAndSaveFileAsync(openFileDialog.FileName);
                }
            }
        }

        private async Task ProcessAndSaveFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Geçerli bir dosya seçilmedi.", "Dosya Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetParentLoading(true, "Alarm listesi dönüştürülüyor, lütfen bekleyin...");
                _viewModel.StatusText = "Alarm listesi dönüştürülüyor, lütfen bekleyin...";
                lblImportDragDrop.Text = $"📩 Seçilen Dosya: {Path.GetFileName(filePath)}\n\nDönüştürülüyor, lütfen bekleyin...";
                lblImportDragDrop.ForeColor = Color.FromArgb(0, 229, 255);

                var records = await Task.Run(() =>
                {
                    return ParsingEngine.ConvertImportedAlarms(filePath);
                });

                if (records.Count == 0)
                {
                    throw new Exception("Dosyada dönüştürülecek geçerli alarm verisi bulunamadı.");
                }

                SetParentLoading(false);

                string defaultOutputName = $"ImportSihirbazı_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Excel Çalışma Kitabı (*.xlsx)|*.xlsx";
                    saveFileDialog.Title = "Dönüştürülmüş Alarm Listesini Kaydet";
                    saveFileDialog.FileName = defaultOutputName;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string targetPath = saveFileDialog.FileName;

                        try
                        {
                            SetParentLoading(true, "Excel dosyası kaydediliyor...");
                            _viewModel.StatusText = "Excel dosyası kaydediliyor...";

                            await Task.Run(() =>
                            {
                                ParsingEngine.SaveToExcel(targetPath, records);
                            });

                            string fileName = Path.GetFileName(targetPath);
                            string successMsg = $"Dönüştürme başarılı: {records.Count} alarm verisi standart formata dönüştürüldü ve {fileName} olarak kaydedildi.";

                            _viewModel.StatusText = successMsg;

                            MessageBox.Show(successMsg, "Dönüştürme Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Kaydetme işlemi esnasında bir hata oluştu:\n\n{ex.Message}", "Kaydetme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            _viewModel.StatusText = "Kaydetme başarısız oldu.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dönüştürme işlemi esnasında bir hata oluştu:\n\n{ex.Message}", "Dönüştürme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _viewModel.StatusText = "Dönüştürme başarısız oldu.";
            }
            finally
            {
                SetParentLoading(false);
                lblImportDragDrop.Text = "📩 1. INPUT: IIoT ENGINEER ALARM IMPORT EXCELİNİ P360 IMPORTUNA HAZIR HALE GETİRMEK İÇİN DOSYAYI BURAYA SÜRÜKLEYİN VEYA TIKLAYIN (.xlsx)";
                lblImportDragDrop.ForeColor = Color.FromArgb(0, 229, 255);
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
