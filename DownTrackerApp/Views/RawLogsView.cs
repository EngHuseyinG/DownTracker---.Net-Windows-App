using System;
using System.Drawing;
using System.Windows.Forms;
using DownTracker.Models;
using DownTracker.ViewModels;

namespace DownTracker.Views
{
    public class RawLogsView : UserControl
    {
        private readonly MainViewModel _mainViewModel;
        private readonly RawLogsViewModel _viewModel;

        private DataGridView dgvRawLogs;

        public RawLogsViewModel ViewModel => _viewModel;

        public RawLogsView(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _viewModel = new RawLogsViewModel(mainViewModel);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(10);

            // Initialize raw logs grid
            dgvRawLogs = new DataGridView
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

            // Configure columns for dgvRawLogs
            dgvRawLogs.Columns.Clear();
            dgvRawLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Zaman Damgası", DataPropertyName = "TagDate", Width = 180 });
            dgvRawLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sinyal Adı (TagName)", DataPropertyName = "TagName", Width = 400 });
            dgvRawLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Değer (Value)", DataPropertyName = "Value", Width = 120 });

            // Apply grid styling
            StyleGrid(dgvRawLogs);

            // Bind data source and formatting
            dgvRawLogs.AutoGenerateColumns = false;
            dgvRawLogs.DataSource = _viewModel.RawLogs;

            // Auto-resize columns
            dgvRawLogs.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            dgvRawLogs.DataBindingComplete += (s, e) => dgvRawLogs.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            // Cell formatting
            dgvRawLogs.CellFormatting += DgvRawLogs_CellFormatting;

            this.Controls.Add(dgvRawLogs);
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

        private void DgvRawLogs_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvRawLogs.Rows.Count) return;

            var row = dgvRawLogs.Rows[e.RowIndex];
            var log = row.DataBoundItem as LogEntry;
            if (log == null) return;

            // Format date cells explicitly using "dd.MM.yyyy HH:mm:ss"
            if (e.ColumnIndex == 0) // TagDate
            {
                e.Value = log.TagDate.ToString("dd.MM.yyyy HH:mm:ss");
                e.FormattingApplied = true;
            }
        }
    }
}
