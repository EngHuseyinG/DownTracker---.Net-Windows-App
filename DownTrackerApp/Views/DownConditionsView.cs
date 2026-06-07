using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DownTracker.Models;
using DownTracker.ViewModels;

namespace DownTracker.Views
{
    public class DownConditionsView : UserControl
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DownConditionsViewModel _viewModel;
        
        private DataGridView dgvDowntimes;
        private DataGridView dgvDowntimeDetails;
        private Label lblDetailsTitle;
        private SplitContainer splitDowntimes;

        public DownConditionsViewModel ViewModel => _viewModel;
        public string CurrentSortColumn => _viewModel.CurrentSortColumn;

        public DownConditionsView(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _viewModel = new DownConditionsViewModel(mainViewModel);
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Dock = DockStyle.Fill;

            // Initialize main grid
            dgvDowntimes = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(18, 18, 18),
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeight = 35,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(60, 60, 60),
                ReadOnly = true,
                RowHeadersVisible = false,
                RowTemplate = { Height = 28 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                TabIndex = 0
            };

            // Configure columns for dgvDowntimes
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Varlık Adı", DataPropertyName = "AssetName", Width = 110 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Başlangıç Zamanı", DataPropertyName = "StartTime", Width = 160, MinimumWidth = 160 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bitiş Zamanı", DataPropertyName = "EndTime", Width = 160, MinimumWidth = 160 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Süre", DataPropertyName = "DurationDisplay", Width = 80 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ait Olduğu Çevrim Aralığı", DataPropertyName = "CycleContextDisplay", Width = 150 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kök Neden Alarm Sinyali / Arıza Detayı", DataPropertyName = "RootCauseAlarm", Width = 300 });

            // Apply styles to main grid
            StyleGrid(dgvDowntimes);

            // Setup SplitContainer and Details Panel
            SetupDowntimeDetailsPanel();

            // Setup bindings and formatting
            dgvDowntimes.AutoGenerateColumns = false;
            dgvDowntimes.DataSource = _viewModel.FilteredDowntimes;
            
            // Auto-resize
            dgvDowntimes.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dgvDowntimes.DataBindingComplete += (s, e) => dgvDowntimes.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            // Cell formatting
            dgvDowntimes.CellFormatting += DgvDowntimes_CellFormatting;

            // Sorting event binding
            BindMainSortingEvents();
        }

        private void StyleGrid(DataGridView dgv)
        {
            dgv.BackgroundColor = Color.FromArgb(18, 18, 18);
            dgv.GridColor = Color.FromArgb(45, 45, 45);
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            // Header Style
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(31, 31, 31);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(224, 224, 224);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(31, 31, 31);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;

            // Rows Style
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(224, 224, 224);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(68, 68, 68);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Alternating Rows
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(28, 28, 28);
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = Color.FromArgb(224, 224, 224);
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(68, 68, 68);
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            dgv.RowHeadersVisible = false;
            dgv.RowTemplate.Height = 28;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = true;
            dgv.ShowCellToolTips = true;
        }

        private void SetupDowntimeDetailsPanel()
        {
            // Create SplitContainer
            splitDowntimes = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 350,
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.FromArgb(224, 224, 224),
                SplitterWidth = 6
            };

            this.Controls.Add(splitDowntimes);

            // Panel1: Contains the main dgvDowntimes
            dgvDowntimes.Dock = DockStyle.Fill;
            splitDowntimes.Panel1.Controls.Add(dgvDowntimes);

            // Panel2: Details Panel
            var pnlDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(0, 5, 0, 0)
            };

            // Details Title Label
            lblDetailsTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI Black", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 118), // Neon Green
                Text = "SEÇİLİ DURUŞUN MİKRO-DURUŞ DETAYLARI (SANİYE SANİYE)",
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlDetails.Controls.Add(lblDetailsTitle);

            // Details DataGridView
            dgvDowntimeDetails = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false
            };

            pnlDetails.Controls.Add(dgvDowntimeDetails);
            pnlDetails.Controls.SetChildIndex(dgvDowntimeDetails, 0); // Put grid below the label

            splitDowntimes.Panel2.Controls.Add(pnlDetails);

            // Configure details grid columns
            dgvDowntimeDetails.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Başlangıç Zamanı", DataPropertyName = "StartTime", Width = 160, MinimumWidth = 160 });
            dgvDowntimeDetails.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bitiş Zamanı", DataPropertyName = "EndTime", Width = 160, MinimumWidth = 160 });
            dgvDowntimeDetails.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Süre", DataPropertyName = "DurationDisplay", Width = 80 });
            dgvDowntimeDetails.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bölünmüş mü?", DataPropertyName = "IsSplit", Width = 100 });
            dgvDowntimeDetails.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Root Cause / Alarm", DataPropertyName = "RootCauseAlarm", Width = 300 });

            // Apply styling
            StyleGrid(dgvDowntimeDetails);

            // Subscribe to cell formatting for the details grid (to highlight alarmed / split micro downtimes)
            dgvDowntimeDetails.CellFormatting += DgvDowntimeDetails_CellFormatting;

            // Subscribe to selection change on the main grid
            dgvDowntimes.SelectionChanged += DgvDowntimes_SelectionChanged;

            // Subscribe to double click for pop-up details
            dgvDowntimes.CellDoubleClick += DgvDowntimes_CellDoubleClick;
        }

        private void DgvDowntimes_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvDowntimes.Rows.Count) return;

            var row = dgvDowntimes.Rows[e.RowIndex];
            var downtime = row.DataBoundItem as DowntimeRecord;
            if (downtime == null) return;

            var col = dgvDowntimes.Columns[e.ColumnIndex];
            if (col.DataPropertyName == "StartTime")
            {
                e.Value = downtime.StartTime.ToString("dd.MM.yyyy HH:mm:ss");
                e.FormattingApplied = true;
            }
            else if (col.DataPropertyName == "EndTime")
            {
                e.Value = downtime.EndTime.ToString("dd.MM.yyyy HH:mm:ss");
                e.FormattingApplied = true;
            }

            if (col.DataPropertyName == "RootCauseAlarm")
            {
                row.Cells[e.ColumnIndex].ToolTipText = downtime.RootCauseAlarm;
            }

            // Color-code the rows based on IsAlarmed state
            if (downtime.IsAlarmed)
            {
                row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 74, 74); // Neon Red `#FF4A4A`
                row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(255, 74, 74);
            }
            else
            {
                row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 112, 67); // Soft Orange `#FF7043`
                row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(255, 112, 67);
            }
        }

        private void DgvDowntimeDetails_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvDowntimeDetails.Rows.Count) return;

            var row = dgvDowntimeDetails.Rows[e.RowIndex];
            var downtime = row.DataBoundItem as DowntimeRecord;
            if (downtime == null) return;

            var col = dgvDowntimeDetails.Columns[e.ColumnIndex];
            if (col.DataPropertyName == "StartTime")
            {
                e.Value = downtime.StartTime.ToString("dd.MM.yyyy HH:mm:ss");
                e.FormattingApplied = true;
            }
            else if (col.DataPropertyName == "EndTime")
            {
                e.Value = downtime.EndTime.ToString("dd.MM.yyyy HH:mm:ss");
                e.FormattingApplied = true;
            }

            if (col.DataPropertyName == "RootCauseAlarm")
            {
                row.Cells[e.ColumnIndex].ToolTipText = downtime.RootCauseAlarm;
            }

            // Color-code the rows based on IsAlarmed state
            if (downtime.IsAlarmed)
            {
                row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 74, 74); // Neon Red `#FF4A4A`
                row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(255, 74, 74);
            }
            else
            {
                row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 112, 67); // Soft Orange `#FF7043`
                row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(255, 112, 67);
            }
        }

        private void DgvDowntimes_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDowntimes.SelectedRows.Count > 0)
            {
                var row = dgvDowntimes.SelectedRows[0];
                var downtime = row.DataBoundItem as DowntimeRecord;
                if (downtime != null)
                {
                    dgvDowntimeDetails.DataSource = downtime.ChildDowntimes;
                }
                else
                {
                    dgvDowntimeDetails.DataSource = null;
                }
            }
            else
            {
                dgvDowntimeDetails.DataSource = null;
            }
        }

        private void DgvDowntimes_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvDowntimes.Rows[e.RowIndex];
            var downtime = row.DataBoundItem as DowntimeRecord;
            if (downtime == null) return;

            using (var detailForm = new FormDowntimeInfo(downtime))
            {
                detailForm.ShowDialog(this.FindForm());
            }
        }

        public void SortMainData(string columnName)
        {
            // Save selection
            var selectedDowntime = dgvDowntimes.SelectedRows.Count > 0
                ? dgvDowntimes.SelectedRows[0].DataBoundItem as DowntimeRecord
                : null;

            _viewModel.SortData(columnName);

            // Update main header arrows
            UpdateMainHeaderUi();

            // Restore selection
            if (selectedDowntime != null)
            {
                foreach (DataGridViewRow row in dgvDowntimes.Rows)
                {
                    if (row.DataBoundItem == selectedDowntime)
                    {
                        row.Selected = true;
                        dgvDowntimes.CurrentCell = row.Cells[0];
                        break;
                    }
                }
            }
        }

        private void UpdateMainHeaderUi()
        {
            string arrow = _viewModel.IsAscending ? " ▲" : " ▼";

            if (dgvDowntimes != null && dgvDowntimes.Columns.Count > 3)
            {
                dgvDowntimes.Columns[1].HeaderText = "Başlangıç Zamanı" + (_viewModel.CurrentSortColumn == "StartTime" ? arrow : "");
                dgvDowntimes.Columns[2].HeaderText = "Bitiş Zamanı" + (_viewModel.CurrentSortColumn == "EndTime" ? arrow : "");
                dgvDowntimes.Columns[3].HeaderText = "Süre" + (_viewModel.CurrentSortColumn == "Duration" ? arrow : "");
            }
        }

        private void BindMainSortingEvents()
        {
            // Bind sorting when clicking headers on dgvDowntimes
            dgvDowntimes.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.ColumnIndex == 1) SortMainData("StartTime");
                else if (e.ColumnIndex == 2) SortMainData("EndTime");
                else if (e.ColumnIndex == 3) SortMainData("Duration");
            };

            // Set column header sort modes to programmatic to prevent default behavior
            if (dgvDowntimes != null)
            {
                foreach (DataGridViewColumn col in dgvDowntimes.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.Programmatic;
                }
            }
        }
    }
}
