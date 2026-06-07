using System;
using System.Drawing;
using System.Windows.Forms;
using DownTracker.Models;
using DownTracker.ViewModels;

namespace DownTracker.Views
{
    public class CycleAnalysisView : UserControl
    {
        private readonly MainViewModel _mainViewModel;
        private readonly CycleAnalysisViewModel _viewModel;
        
        private DataGridView dgvCycles;

        public CycleAnalysisViewModel ViewModel => _viewModel;

        public CycleAnalysisView(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _viewModel = new CycleAnalysisViewModel(mainViewModel);
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(10);

            // Initialize cycles grid
            dgvCycles = new DataGridView
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

            // Configure columns for dgvCycles
            dgvCycles.Columns.Clear();
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Varlık Adı",
                DataPropertyName = "StationName",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Çevrim No", DataPropertyName = "Index", Width = 80 });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Başlangıç (TransEnd)", DataPropertyName = "StartTime", Width = 180, MinimumWidth = 180 });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bitiş (TransEnd)", DataPropertyName = "EndTime", Width = 180, MinimumWidth = 180 });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Çevrim Süresi", DataPropertyName = "DurationDisplay", Width = 120 });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Duruş Adedi", DataPropertyName = "DowntimeCount", Width = 100 });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Toplam Duruş Süresi", DataPropertyName = "TotalDowntimeSeconds", Width = 150 });

            // Apply grid styling
            StyleGrid(dgvCycles);

            // Bind data source and formatting
            dgvCycles.AutoGenerateColumns = false;
            dgvCycles.DataSource = _viewModel.CurrentCycles;
            
            // Auto-resize columns
            dgvCycles.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dgvCycles.DataBindingComplete += (s, e) => dgvCycles.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            // Cell formatting
            dgvCycles.CellFormatting += DgvCycles_CellFormatting;

            this.Controls.Add(dgvCycles);
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

        private void DgvCycles_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvCycles.Rows.Count) return;

            var row = dgvCycles.Rows[e.RowIndex];
            var cycle = row.DataBoundItem as Cycle;
            if (cycle == null) return;

            // Format date cells explicitly using "dd.MM.yyyy HH:mm:ss"
            var col = dgvCycles.Columns[e.ColumnIndex];
            if (col.DataPropertyName == "StartTime")
            {
                e.Value = cycle.StartTime.ToString("dd.MM.yyyy HH:mm:ss");
                e.FormattingApplied = true;
            }
            else if (col.DataPropertyName == "EndTime")
            {
                e.Value = cycle.EndTime.ToString("dd.MM.yyyy HH:mm:ss");
                e.FormattingApplied = true;
            }

            // Highlight cycle rows in Neon Green (#00E676)
            row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 230, 118);
            row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(0, 230, 118);
        }
    }
}
