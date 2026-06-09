using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DownTracker.Models;

namespace DownTracker.Views
{
    public class FormDowntimeInfo : Form
    {
        private readonly DowntimeRecord _downtime;
        private Panel pnlTimelineCanvas;
        private FlowLayoutPanel flpCardContainer;
        private FlowLayoutPanel pnlHeaderRow; // Promoted to class-level field to access in sorting methods
        private Label lblHeaderStart;
        private Label lblHeaderEnd;
        private Label lblHeaderDuration;
        private Label lblHeaderDesc;
        private Button btnClose;
        private readonly List<Panel> _cards = new List<Panel>();

        private readonly List<Tuple<Rectangle, string>> _hoverRegions = new List<Tuple<Rectangle, string>>();
        private Label lblCentralSignalInfo;
        private int _selectedSegmentIndex = -1;

        private bool _isAscending = true;
        private string _currentSortColumn = "StartTime"; // İlk açılış varsayılanı
        private List<DowntimeRecordVM> _allConditions = new List<DowntimeRecordVM>();

        public FormDowntimeInfo(DowntimeRecord downtime)
        {
            _downtime = downtime;
            InitializeRuntimeForm();
        }

        private void InitializeRuntimeForm()
        {
            // 1. Form Basic Properties
            this.Text = $"Duruş Detay ve Zaman Çizelgesi - {_downtime.AssetName}";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(15, 15, 15);
            this.ForeColor = Color.FromArgb(224, 224, 224);

            // 2. Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(26, 26, 26),
                Padding = new Padding(15)
            };

            var lblAsset = new Label
            {
                Text = $"VARLIK: {_downtime.AssetName} | GRUP: {_downtime.StationGroup}",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 118),
                Location = new Point(15, 12),
                AutoSize = true
            };

            var lblAlarm = new Label
            {
                Text = $"Kök Neden Alarm: {_downtime.RootCauseAlarm}",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = _downtime.IsAlarmed ? Color.FromArgb(255, 74, 74) : Color.FromArgb(255, 112, 67),
                Location = new Point(15, 38),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblAsset);
            pnlHeader.Controls.Add(lblAlarm);
            this.Controls.Add(pnlHeader);

            // 3. Timeline Canvas Panel
            pnlTimelineCanvas = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(24, 24, 24)
            };
            pnlTimelineCanvas.Paint += PnlTimelineCanvas_Paint;
            pnlTimelineCanvas.MouseMove += PnlTimelineCanvas_MouseMove;
            this.Controls.Add(pnlTimelineCanvas);

            // 3.5 Central Signal Summary Panel
            var pnlSignalSummary = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(25, 25, 25),
                Padding = new Padding(15, 5, 15, 5)
            };

            lblCentralSignalInfo = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(0, 229, 255), // Neon Turquoise
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "İncelemek istediğiniz arıza veya alarm çizgisinin üzerine gelin...",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            pnlSignalSummary.Controls.Add(lblCentralSignalInfo);
            this.Controls.Add(pnlSignalSummary);

            // 4. Grid Title Label
            var lblGridTitle = new Label
            {
                Text = "MİKRO DURUŞ SEGMENTLERİ (SANİYE SANİYE)",
                Font = new Font("Segoe UI Black", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 118),
                Height = 30,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 5, 0, 0)
            };
            this.Controls.Add(lblGridTitle);

            // Column Headers Row for Card Alignment
            pnlHeaderRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(25, 25, 25),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(25, 3, 10, 3), // Indented to match card padding
                Margin = new Padding(0)
            };
            lblHeaderStart = new Label { Name = "lblHeaderStart", Text = "Başlangıç Zamanı", Size = new Size(150, 20), ForeColor = Color.FromArgb(0, 229, 255), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lblHeaderEnd = new Label { Name = "lblHeaderEnd", Text = "Bitiş Zamanı", Size = new Size(150, 20), ForeColor = Color.FromArgb(0, 229, 255), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lblHeaderDuration = new Label { Name = "lblHeaderDuration", Text = "Süre", Size = new Size(80, 20), ForeColor = Color.FromArgb(0, 229, 255), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lblHeaderDesc = new Label { Name = "lblHeaderDesc", Text = "Alarm / Durum Açıklaması", Size = new Size(570, 20), ForeColor = Color.FromArgb(0, 229, 255), Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            pnlHeaderRow.Controls.Add(lblHeaderStart);
            pnlHeaderRow.Controls.Add(lblHeaderEnd);
            pnlHeaderRow.Controls.Add(lblHeaderDuration);
            pnlHeaderRow.Controls.Add(lblHeaderDesc);
            this.Controls.Add(pnlHeaderRow);

            // 5. FlowLayoutPanel Setup
            flpCardContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(15, 15, 15),
                Padding = new Padding(15, 10, 15, 10)
            };
            this.Controls.Add(flpCardContainer);

            // 6. Bottom Panel (Close Button)
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(26, 26, 26),
                Padding = new Padding(10)
            };

            btnClose = new Button
            {
                Text = "Kapat",
                Width = 100,
                Height = 30,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(85, 85, 85);
            btnClose.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(btnClose);
            this.Controls.Add(pnlBottom);

            // Z-Order layout ordering (Bottom up)
            pnlBottom.BringToFront();
            pnlHeader.BringToFront();
            pnlTimelineCanvas.BringToFront();
            pnlSignalSummary.BringToFront();
            lblGridTitle.BringToFront();
            pnlHeaderRow.BringToFront();
            flpCardContainer.BringToFront();

            this.Load += FormDowntimeInfo_Load;
        }

        private void FormDowntimeInfo_Load(object sender, EventArgs e)
        {
            // 1. Önce kartların veri havuzunu doldur
            PopulateCards();

            // 2. Olayları ekrandaki nesnelere bağla
            BindSortingEvents();

            // 3. İlk yükleme sıralaması: Verileri eskiden yeniye kronolojik diz, oku bas ve kartları çiz
            _currentSortColumn = "StartTime";
            _isAscending = true;

            // Doğrudan switch mekanizmasını tetiklemesi ve UI'ı ekrana basması için SortData'yı çağır
            SortData("StartTime");
        }

        private void PopulateCards()
        {
            _allConditions.Clear();
            if (_downtime.ChildDowntimes != null && _downtime.ChildDowntimes.Count > 0)
            {
                for (int i = 0; i < _downtime.ChildDowntimes.Count; i++)
                {
                    var child = _downtime.ChildDowntimes[i];
                    _allConditions.Add(new DowntimeRecordVM
                    {
                        StartTime = child.StartTime,
                        EndTime = child.EndTime,
                        DurationDisplay = child.DurationDisplay,
                        IsSplitDisplay = child.IsSplit ? "Bölünmüş Parça" : "Tekil Duruş",
                        RootCauseAlarm = child.RootCauseAlarm,
                        IsAlarmed = child.IsAlarmed,
                        OriginalIndex = i
                    });
                }
            }
            else
            {
                _allConditions.Add(new DowntimeRecordVM
                {
                    StartTime = _downtime.StartTime,
                    EndTime = _downtime.EndTime,
                    DurationDisplay = _downtime.DurationDisplay,
                    IsSplitDisplay = "Tekil Duruş",
                    RootCauseAlarm = _downtime.RootCauseAlarm,
                    IsAlarmed = _downtime.IsAlarmed,
                    OriginalIndex = 0
                });
            }
        }

        private void RefreshDrawnCards(List<DowntimeRecordVM> records)
        {
            flpCardContainer.Controls.Clear();
            _cards.Clear();

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                int originalIdx = record.OriginalIndex;

                // Create the Card flow panel
                var card = new FlowLayoutPanel
                {
                    Size = new Size(950, 35),
                    Margin = new Padding(0, 0, 0, 6),
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    Padding = new Padding(10, 5, 10, 5),
                    Cursor = Cursors.Hand,
                    Tag = new CardMetadata { Index = originalIdx, IsAlarm = record.IsAlarmed }
                };

                // Add Labels with fixed widths (150, 150, 80, 420)
                var lblStart = new Label
                {
                    Text = record.StartTime.ToString("dd.MM.yyyy HH:mm:ss"),
                    Size = new Size(150, 25),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    Margin = new Padding(0)
                };

                var lblEnd = new Label
                {
                    Text = record.EndTime.ToString("dd.MM.yyyy HH:mm:ss"),
                    Size = new Size(150, 25),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    Margin = new Padding(0)
                };

                var lblDuration = new Label
                {
                    Text = record.DurationDisplay,
                    Size = new Size(80, 25),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Margin = new Padding(0)
                };

                var lblDesc = new Label
                {
                    Name = "lblDesc",
                    Text = record.RootCauseAlarm,
                    Size = new Size(570, 25),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    Margin = new Padding(0)
                };

                card.Controls.Add(lblStart);
                card.Controls.Add(lblEnd);
                card.Controls.Add(lblDuration);
                card.Controls.Add(lblDesc);

                // Wire up click event handlers for the card and its labels
                EventHandler clickHandler = (s, ev) =>
                {
                    _selectedSegmentIndex = originalIdx;
                    pnlTimelineCanvas.Invalidate(); // Highlights segment in GDI+ canvas
                    RefreshCardColors();           // Updates card backgrounds and heights
                };

                card.Click += clickHandler;
                lblStart.Click += clickHandler;
                lblEnd.Click += clickHandler;
                lblDuration.Click += clickHandler;
                lblDesc.Click += clickHandler;

                flpCardContainer.Controls.Add(card);
                _cards.Add(card);
            }

            // Perform initial coloration
            RefreshCardColors();
        }

        private void SortData(string columnName)
        {
            if (_currentSortColumn == columnName) { _isAscending = !_isAscending; }
            else { _currentSortColumn = columnName; _isAscending = true; }

            // 1. Adım: Önce ekrandaki gerçek başlık yazılarını ve oklarını güncelle
            UpdateHeaderUi();

            // 2. Adım: Havuzdaki verileri katı tiple sırala
            switch (columnName)
            {
                case "StartTime":
                    _allConditions = _isAscending ? _allConditions.OrderBy(x => x.StartTime).ToList() : _allConditions.OrderByDescending(x => x.StartTime).ToList();
                    break;
                case "EndTime":
                    _allConditions = _isAscending ? _allConditions.OrderBy(x => x.EndTime).ToList() : _allConditions.OrderByDescending(x => x.EndTime).ToList();
                    break;
                case "Duration":
                    _allConditions = _isAscending ? _allConditions.OrderBy(x => x.Duration).ToList() : _allConditions.OrderByDescending(x => x.Duration).ToList();
                    break;
                case "AlarmDescription":
                    _allConditions = _isAscending ? _allConditions.OrderBy(x => x.AlarmDescription).ToList() : _allConditions.OrderByDescending(x => x.AlarmDescription).ToList();
                    break;
            }

            // 3. Adım: Kartları temizle ve yeni sıralamayla FlowLayoutPanel'e yeniden çiz
            RefreshDrawnCards(_allConditions);
        }

        private void UpdateHeaderUi()
        {
            string arrow = _isAscending ? " ▲" : " ▼";

            // Sınıf seviyesindeki referanslar üzerinden doğrudan fiziksel müdahale
            if (lblHeaderStart != null) lblHeaderStart.Text = "Başlangıç Zamanı" + (_currentSortColumn == "StartTime" ? arrow : "");
            if (lblHeaderEnd != null) lblHeaderEnd.Text = "Bitiş Zamanı" + (_currentSortColumn == "EndTime" ? arrow : "");
            if (lblHeaderDuration != null) lblHeaderDuration.Text = "Süre" + (_currentSortColumn == "Duration" ? arrow : "");
            if (lblHeaderDesc != null) lblHeaderDesc.Text = "Alarm / Durum Açıklaması" + (_currentSortColumn == "AlarmDescription" ? arrow : "");

            // Paneli yeniden çizilmeye zorla (Force Render)
            pnlHeaderRow.Invalidate();
            pnlHeaderRow.Update();
        }

        private bool _sortingEventsBound = false;

        private void BindSortingEvents()
        {
            if (_sortingEventsBound) return;
            _sortingEventsBound = true;

            // 1. Yol: Panel içi tarama
            bool matchedStart = false, matchedEnd = false, matchedDuration = false, matchedDesc = false;
            foreach (Control ctrl in pnlHeaderRow.Controls)
            {
                if (ctrl is Label lbl)
                {
                    lbl.Cursor = Cursors.Hand; // Üzerine gelince el işareti yap

                    if (lbl.Name == "lblHeaderStart" || lbl.Text.Contains("Başlangıç"))
                    {
                        lbl.Click += (s, e) => SortData("StartTime");
                        matchedStart = true;
                    }
                    else if (lbl.Name == "lblHeaderEnd" || lbl.Text.Contains("Bitiş"))
                    {
                        lbl.Click += (s, e) => SortData("EndTime");
                        matchedEnd = true;
                    }
                    else if (lbl.Name == "lblHeaderDuration" || lbl.Text.Contains("Süre"))
                    {
                        lbl.Click += (s, e) => SortData("Duration");
                        matchedDuration = true;
                    }
                    else if (lbl.Name == "lblHeaderDesc" || lbl.Text.Contains("Alarm"))
                    {
                        lbl.Click += (s, e) => SortData("AlarmDescription");
                        matchedDesc = true;
                    }
                }
            }

            // 2. Yol (Garanti): Eğer bu label nesneleri sınıf seviyesinde tanımlıysa doğrudan bağla (sadece 1. yol ile eşleşmemişse)
            if (!matchedStart && lblHeaderStart != null) { lblHeaderStart.Cursor = Cursors.Hand; lblHeaderStart.Click += (s, e) => SortData("StartTime"); }
            if (!matchedEnd && lblHeaderEnd != null) { lblHeaderEnd.Cursor = Cursors.Hand; lblHeaderEnd.Click += (s, e) => SortData("EndTime"); }
            if (!matchedDuration && lblHeaderDuration != null) { lblHeaderDuration.Cursor = Cursors.Hand; lblHeaderDuration.Click += (s, e) => SortData("Duration"); }
            if (!matchedDesc && lblHeaderDesc != null) { lblHeaderDesc.Cursor = Cursors.Hand; lblHeaderDesc.Click += (s, e) => SortData("AlarmDescription"); }
        }

        private void RefreshCardColors()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card.Tag is CardMetadata meta)
                {
                    var lblDesc = card.Controls["lblDesc"] as Label;
                    if (meta.Index == _selectedSegmentIndex)
                    {
                        // SEÇİLEN KART: İster alarm ister duruş olsun, Canlı Neon Pembe parlayacak ve dikeyde esneyecek!
                        card.Height = 55;
                        if (lblDesc != null)
                        {
                            lblDesc.AutoSize = false;
                            lblDesc.Size = new Size(570, 45); // Wrap description
                        }
                        card.BackColor = Color.FromArgb(255, 0, 85);
                        foreach (Control ctrl in card.Controls) ctrl.ForeColor = Color.White;
                    }
                    else
                    {
                        // SEÇİLMEYEN TÜM KARTLAR: Çizelgedeki gibi standart net kırmızı (#FF4A4A) başlayacak!
                        card.Height = 35;
                        if (lblDesc != null)
                        {
                            lblDesc.AutoSize = false;
                            lblDesc.Size = new Size(570, 25);
                        }
                        card.BackColor = Color.FromArgb(255, 74, 74);
                        foreach (Control ctrl in card.Controls) ctrl.ForeColor = Color.FromArgb(20, 20, 20);
                    }
                }
            }
        }

        private class CardMetadata
        {
            public int Index { get; set; }
            public bool IsAlarm { get; set; }
        }



        private void PnlTimelineCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var match = _hoverRegions.FirstOrDefault(r => r.Item1.Contains(e.Location));
            if (match != null)
            {
                // Fare çizelgede bir verinin üzerindeyse bilgiyi tepe panelinde göster
                lblCentralSignalInfo.Text = match.Item2;
            }
            else
            {
                // Fare boşluğa çıktığı an panel tertemiz varsayılan durumuna döner
                lblCentralSignalInfo.Text = "İncelemek istediğiniz arıza veya alarm çizgisinin üzerine gelin...";
            }
        }

        private void PnlTimelineCanvas_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            _hoverRegions.Clear();

            int width = pnlTimelineCanvas.Width;
            int drawWidth = width - 30;
            int barY = 22;
            int barHeight = 16;

            long cycleStartTicks = _downtime.CycleStartTime.Ticks;
            long cycleEndTicks = _downtime.CycleEndTime.Ticks;

            if (cycleStartTicks == 0 || cycleEndTicks == 0)
            {
                cycleStartTicks = _downtime.StartTime.AddMinutes(-2).Ticks;
                cycleEndTicks = _downtime.EndTime.AddMinutes(2).Ticks;
            }

            long totalTicks = cycleEndTicks - cycleStartTicks;
            if (totalTicks <= 0) totalTicks = 1;

            // 1. Draw cycle base bar (gray)
            using (var baseBrush = new SolidBrush(Color.FromArgb(45, 45, 45)))
            {
                g.FillRectangle(baseBrush, 15, barY, drawWidth, barHeight);
            }

            // 2. Draw down periods (red / flash pink)
            using (var redBrush = new SolidBrush(Color.FromArgb(255, 74, 74)))
            using (var highlightBrush = new SolidBrush(Color.FromArgb(255, 0, 85)))
            {
                if (_downtime.ChildDowntimes != null && _downtime.ChildDowntimes.Count > 0)
                {
                    for (int i = 0; i < _downtime.ChildDowntimes.Count; i++)
                    {
                        var child = _downtime.ChildDowntimes[i];
                        long start = child.StartTime.Ticks;
                        long end = child.EndTime.Ticks;

                        float x1 = 15 + ((float)(start - cycleStartTicks) / totalTicks) * drawWidth;
                        float x2 = 15 + ((float)(end - cycleStartTicks) / totalTicks) * drawWidth;
                        float w = x2 - x1;
                        if (w < 2.0f) w = 2.0f;

                        var activeBrush = (i == _selectedSegmentIndex) ? highlightBrush : redBrush;
                        g.FillRectangle(activeBrush, x1, barY, w, barHeight);

                        string info = $"[DURUŞ SEGMENTİ] Başlangıç: {child.StartTime:dd.MM.yyyy HH:mm:ss} | Bitiş: {child.EndTime:dd.MM.yyyy HH:mm:ss} | Süre: {child.DurationDisplay} | Sinyal: {child.RootCauseAlarm}";
                        _hoverRegions.Add(new Tuple<Rectangle, string>(
                            new Rectangle((int)x1, barY, (int)w, barHeight),
                            info
                        ));
                    }
                }
                else
                {
                    long start = _downtime.StartTime.Ticks;
                    long end = _downtime.EndTime.Ticks;

                    float x1 = 15 + ((float)(start - cycleStartTicks) / totalTicks) * drawWidth;
                    float x2 = 15 + ((float)(end - cycleStartTicks) / totalTicks) * drawWidth;
                    float w = x2 - x1;
                    if (w < 2.0f) w = 2.0f;

                    var activeBrush = (_selectedSegmentIndex == 0) ? highlightBrush : redBrush;
                    g.FillRectangle(activeBrush, x1, barY, w, barHeight);

                    string info = $"[DURUŞ] Başlangıç: {_downtime.StartTime:dd.MM.yyyy HH:mm:ss} | Bitiş: {_downtime.EndTime:dd.MM.yyyy HH:mm:ss} | Süre: {_downtime.DurationDisplay} | Sinyal: {_downtime.RootCauseAlarm}";
                    _hoverRegions.Add(new Tuple<Rectangle, string>(
                        new Rectangle((int)x1, barY, (int)w, barHeight),
                        info
                    ));
                }
            }

            // 3. Draw alarm markers (yellow)
            if (_downtime.ChildDowntimes != null)
            {
                foreach (var child in _downtime.ChildDowntimes)
                {
                    if (child.IsAlarmed)
                    {
                        long alarmTime = child.StartTime.Ticks;
                        float ax = 15 + ((float)(alarmTime - cycleStartTicks) / totalTicks) * drawWidth;

                        // Draw vertical dashed line
                        using (var pen = new Pen(Color.FromArgb(255, 214, 0), 3.0f))
                        {
                            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                            g.DrawLine(pen, ax, barY - 6, ax, barY + barHeight + 6);
                        }

                        // Draw a small yellow triangle
                        PointF[] triangle = new PointF[]
                        {
                            new PointF(ax, barY - 6),
                            new PointF(ax - 6, barY - 14),
                            new PointF(ax + 6, barY - 14)
                        };
                        using (var yellowBrush = new SolidBrush(Color.FromArgb(255, 214, 0)))
                        {
                            g.FillPolygon(yellowBrush, triangle);
                        }

                        string alarmDetailedInfo = $"[ALARM] Başlangıç: {child.StartTime:HH:mm:ss} | Bitiş: {child.EndTime:HH:mm:ss} | Süre: {child.Duration.TotalSeconds:F1}s | Sinyal: {child.RootCauseAlarm}";
                        _hoverRegions.Add(new Tuple<Rectangle, string>(
                            new Rectangle((int)ax - 7, barY - 14, 14, barHeight + 20),
                            alarmDetailedInfo
                        ));
                    }
                }
            }

            // 4. Draw texts
            float leftX = 15;
            float rightX = width - 15;
            float bottomY = pnlTimelineCanvas.Height - 15;

            using (var textFont = new Font("Segoe UI", 8F, FontStyle.Bold))
            using (var whiteBrush = new SolidBrush(Color.White))
            using (var font = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb(160, 160, 160)))
            {
                string startText = _downtime.CycleStartTime == default ? _downtime.StartTime.AddMinutes(-2).ToString("HH:mm:ss") : _downtime.CycleStartTime.ToString("HH:mm:ss");
                string endText = _downtime.CycleEndTime == default ? _downtime.EndTime.AddMinutes(2).ToString("HH:mm:ss") : _downtime.CycleEndTime.ToString("HH:mm:ss");

                // Draw Left Stack
                g.DrawString("TransactionEnd (Başlangıç)", textFont, whiteBrush, leftX, bottomY - 18);
                g.DrawString(startText, font, brush, leftX, bottomY);

                // Draw Right Stack
                string endLabel = "TransactionEnd (Bitiş)";
                var labelSize = g.MeasureString(endLabel, textFont);
                var endSize = g.MeasureString(endText, font);

                g.DrawString(endLabel, textFont, whiteBrush, rightX - labelSize.Width, bottomY - 18);
                g.DrawString(endText, font, brush, rightX - endSize.Width, bottomY);

                // Center text
                double cycleSec = _downtime.CycleStartTime == default ? (_downtime.EndTime - _downtime.StartTime).TotalSeconds + 240 : (_downtime.CycleEndTime - _downtime.CycleStartTime).TotalSeconds;
                string durationText = $"Toplam Çevrim: {cycleSec:F1}s | Duruş Süresi: {_downtime.DurationDisplay}";
                var durSize = g.MeasureString(durationText, font);
                g.DrawString(durationText, font, brush, (width - durSize.Width) / 2, bottomY);
            }
        }
    }

    // Helper VM class for DataGridView binding
    public class DowntimeRecordVM
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string DurationDisplay { get; set; }
        public string IsSplitDisplay { get; set; }
        public string RootCauseAlarm { get; set; }
        public bool IsAlarmed { get; set; }

        public TimeSpan Duration => EndTime - StartTime;
        public string AlarmDescription => RootCauseAlarm;
        public int OriginalIndex { get; set; }
    }
}
