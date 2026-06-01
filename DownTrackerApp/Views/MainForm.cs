using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DownTracker.Models;
using DownTracker.ViewModels;

namespace DownTracker.Views
{
    public partial class MainForm : Form
    {
        private readonly MainViewModel _viewModel;
        private bool _isSyncingSelection = false;
        private DataGridView dgvDowntimeDetails;
        private Label lblBreadcrumb;
        private Panel pnlBreadcrumb;
        private Label lblKPITotal, lblKPIAlarmed, lblKPIUnalarmed;
        private Label lblSelectedTotal, lblSelectedAlarmed, lblSelectedUnalarmed;

        // Grafana Query Wizard Controls
        private TabPage tpGrafanaWizard;
        private TextBox txtDate;
        private TextBox txtChannel;
        private TextBox txtAsset;
        private CheckBox chkAllAssets;
        private RichTextBox txtSqlQuery;
        private Button btnCopyQuery;
        private Label lblCopyStatus;

        public MainForm()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();

            // Configure SplitContainer panels for minimalist look
            splitMain.Panel1MinSize = 150;
            splitMain.SplitterDistance = 180;

            // Enable Drag and Drop on the top panel and label
            pnlTop.AllowDrop = true;
            lblDragDrop.AllowDrop = true;

            pnlTop.DragEnter += LblDragDrop_DragEnter;
            pnlTop.DragLeave += LblDragDrop_DragLeave;
            pnlTop.DragDrop += LblDragDrop_DragDrop;

            lblDragDrop.DragEnter += LblDragDrop_DragEnter;
            lblDragDrop.DragLeave += LblDragDrop_DragLeave;
            lblDragDrop.DragDrop += LblDragDrop_DragDrop;

            SetupDataGridViewStyles();
            SetupBindings();
            SetupBreadcrumbPanel();
            SetupKPISummaryPanel();
            SetupLeftPanelLayout();
            SetupDowntimeDetailsPanel();
            SetupGrafanaQueryWizardTab();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set initial state
            _viewModel.RefreshData();
            UpdateKPISummary();
        }

        private void SetupBindings()
        {
            // Data binding for file path label
            lblFilePath.DataBindings.Add("Text", _viewModel, nameof(_viewModel.FilePath), true, DataSourceUpdateMode.Never, "Yüklü Dosya: Yok");

            // Subscribe to ViewModel notifications
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.StatusText))
                {
                    lblStatus.Text = _viewModel.StatusText;
                }
                else if (e.PropertyName == nameof(_viewModel.SelectedFilterType) ||
                         e.PropertyName == nameof(_viewModel.SelectedGroupName) ||
                         e.PropertyName == nameof(_viewModel.SelectedStationName) ||
                         e.PropertyName == nameof(_viewModel.SelectedRobotName))
                {
                    SyncTreeViewSelection();
                    UpdateBreadcrumbText();
                }
            };

            // Set grid data sources
            dgvDowntimes.AutoGenerateColumns = false;
            dgvCycles.AutoGenerateColumns = false;
            dgvRawLogs.AutoGenerateColumns = false;

            dgvDowntimes.DataSource = _viewModel.FilteredDowntimes;
            dgvDowntimes.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dgvDowntimes.DataBindingComplete += (s, e) => dgvDowntimes.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            dgvCycles.DataSource = _viewModel.CurrentCycles;
            dgvCycles.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dgvCycles.DataBindingComplete += (s, e) => dgvCycles.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            dgvRawLogs.DataSource = _viewModel.RawLogs;
            dgvRawLogs.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            dgvRawLogs.DataBindingComplete += (s, e) => dgvRawLogs.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            ConfigureGridColumns();
        }

        private void SetupDataGridViewStyles()
        {
            StyleGrid(dgvDowntimes);
            StyleGrid(dgvCycles);
            StyleGrid(dgvRawLogs);
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

        private void ConfigureGridColumns()
        {
            // Configure columns for dgvDowntimes
            dgvDowntimes.Columns.Clear();
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Varlık Adı", DataPropertyName = "AssetName", Width = 110 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Başlangıç Zamanı", DataPropertyName = "StartTime", Width = 140 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bitiş Zamanı", DataPropertyName = "EndTime", Width = 140 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Süre", DataPropertyName = "DurationDisplay", Width = 80 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ait Olduğu Çevrim Aralığı", DataPropertyName = "CycleContextDisplay", Width = 150 });
            dgvDowntimes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kök Neden Alarm Sinyali / Arıza Detayı", DataPropertyName = "RootCauseAlarm", Width = 300 });

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
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Başlangıç (TransEnd)", DataPropertyName = "StartTime", Width = 180 });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bitiş (TransEnd)", DataPropertyName = "EndTime", Width = 180 });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Çevrim Süresi", DataPropertyName = "DurationDisplay", Width = 120 });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Duruş Adedi", DataPropertyName = "DowntimeCount", Width = 100 });
            dgvCycles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Toplam Duruş Süresi", DataPropertyName = "TotalDowntimeSeconds", Width = 150 });

            // Configure columns for dgvRawLogs
            dgvRawLogs.Columns.Clear();
            dgvRawLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Zaman Damgası", DataPropertyName = "TagDate", Width = 180 });
            dgvRawLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sinyal Adı (TagName)", DataPropertyName = "TagName", Width = 400 });
            dgvRawLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Değer (Value)", DataPropertyName = "Value", Width = 120 });
        }

        private void LblDragDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                pnlTop.BackColor = Color.FromArgb(45, 45, 45);
                lblDragDrop.ForeColor = Color.FromArgb(0, 230, 118);
            }
        }

        private void LblDragDrop_DragLeave(object sender, EventArgs e)
        {
            pnlTop.BackColor = Color.FromArgb(31, 31, 31);
        }

        private void LblDragDrop_DragDrop(object sender, DragEventArgs e)
        {
            pnlTop.BackColor = Color.FromArgb(31, 31, 31);
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                LoadFile(files[0]);
            }
        }

        private void lblDragDrop_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV Dosyaları (*.csv)|*.csv|Tüm Dosyalar (*.*)|*.*";
                openFileDialog.Title = "Duruş Log Dosyasını Seçin";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadFile(openFileDialog.FileName);
                }
            }
        }

        private void LoadFile(string filePath)
        {
            try
            {
                // Bind directly to LoadAndProcessData as requested
                bool success = _viewModel.LoadAndProcessData(filePath);
                if (success)
                {
                    PopulateTreeView();
                    
                    // Collapse tree and expand only the root node on initial load
                    tvStations.CollapseAll();
                    if (tvStations.Nodes.Count > 0)
                    {
                        tvStations.Nodes[0].Expand();
                    }

                    UpdateKPISummary();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sistem Hatası Yakalandı!\n\nMesaj: {ex.Message}\n\nHata Yeri: {ex.TargetSite}\n\nDetay: {ex.ToString()}", "Kök Hata Raporu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateTreeView(string filterText = "")
        {
            tvStations.BeginUpdate();
            try
            {
                tvStations.Nodes.Clear();

                // 1. Add root node
                var rootNode = new TreeNode("Tüm Varlıklar")
                {
                    Tag = "ALL",
                    Name = "NODE_ALL"
                };
                tvStations.Nodes.Add(rootNode);

                if (_viewModel.StationsList == null) return;

                string query = filterText?.Trim();
                bool hasFilter = !string.IsNullOrEmpty(query);

                // Group stations by group
                var stationsByGroup = new Dictionary<string, List<string>>();
                foreach (var station in _viewModel.StationsList)
                {
                    if (string.IsNullOrEmpty(station)) continue;

                    string group = "Unknown Group";
                    if (_viewModel.StationGroups.TryGetValue(station, out var g) && !string.IsNullOrEmpty(g))
                    {
                        group = g;
                    }

                    // Check if group, station, or any robot name matches filterText
                    bool matchesFilter = !hasFilter;
                    if (hasFilter)
                    {
                        if (group.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            station.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            matchesFilter = true;
                        }
                        else if (_viewModel.StationRobots != null &&
                                 _viewModel.StationRobots.TryGetValue(station, out var robots) &&
                                 robots != null &&
                                 robots.Any(r => r.Contains(query, StringComparison.OrdinalIgnoreCase)))
                        {
                            matchesFilter = true;
                        }
                    }

                    if (matchesFilter)
                    {
                        if (!stationsByGroup.ContainsKey(group))
                        {
                            stationsByGroup[group] = new List<string>();
                        }
                        stationsByGroup[group].Add(station);
                    }
                }

                // Populate groups -> stations -> robots
                var sortedGroups = stationsByGroup.Keys.OrderBy(k => k).ToList();
                foreach (var groupName in sortedGroups)
                {
                    string groupKey = $"GROUP:{groupName}";
                    TreeNode groupNode;
                    if (!rootNode.Nodes.ContainsKey(groupKey))
                    {
                        groupNode = new TreeNode(groupName)
                        {
                            Name = groupKey,
                            Tag = groupKey
                        };
                        rootNode.Nodes.Add(groupNode);
                    }
                    else
                    {
                        groupNode = rootNode.Nodes[groupKey];
                    }

                    var stations = stationsByGroup[groupName].OrderBy(s => s).ToList();
                    foreach (var station in stations)
                    {
                        string stationKey = $"STATION:{groupName}:{station}";
                        TreeNode stationNode;
                        if (!groupNode.Nodes.ContainsKey(stationKey))
                        {
                            stationNode = new TreeNode(station)
                            {
                                Name = stationKey,
                                Tag = stationKey
                            };
                            groupNode.Nodes.Add(stationNode);
                        }
                        else
                        {
                            stationNode = groupNode.Nodes[stationKey];
                        }

                        // Add robots under station
                        if (_viewModel.StationRobots != null && _viewModel.StationRobots.TryGetValue(station, out var robots) && robots != null)
                        {
                            foreach (var robot in robots)
                            {
                                if (string.IsNullOrEmpty(robot)) continue;

                                bool showRobot = !hasFilter ||
                                                 groupName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                                 station.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                                 robot.Contains(query, StringComparison.OrdinalIgnoreCase);

                                if (showRobot)
                                {
                                    string robotKey = $"ROBOT:{groupName}:{station}:{robot}";
                                    if (!stationNode.Nodes.ContainsKey(robotKey))
                                    {
                                        var robotNode = new TreeNode(robot)
                                        {
                                            Name = robotKey,
                                            Tag = robotKey
                                        };
                                        stationNode.Nodes.Add(robotNode);
                                    }
                                }
                            }
                        }
                    }
                }

                tvStations.ExpandAll();
                SyncTreeViewSelection();

                // Reset scrollbar to top if nodes exist (Excel/CSV loaded or search updated)
                if (tvStations.Nodes.Count > 0)
                {
                    tvStations.TopNode = tvStations.Nodes[0];
                }
            }
            finally
            {
                tvStations.EndUpdate();
            }
        }

        private void tvStations_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_isSyncingSelection) return;
            if (e.Node == null) return;

            string tag = e.Node.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            var parts = tag.Split(':');
            string type = parts[0];

            if (type == "ALL")
            {
                _viewModel.SelectedFilterType = "All";
                _viewModel.SelectedGroupName = null;
                _viewModel.SelectedStationName = null;
                _viewModel.SelectedRobotName = null;
            }
            else if (type == "GROUP")
            {
                _viewModel.SelectedFilterType = "Group";
                _viewModel.SelectedGroupName = parts[1];
                _viewModel.SelectedStationName = null;
                _viewModel.SelectedRobotName = null;
            }
            else if (type == "STATION")
            {
                _viewModel.SelectedFilterType = "Station";
                _viewModel.SelectedGroupName = parts[1];
                _viewModel.SelectedStationName = parts[2];
                _viewModel.SelectedRobotName = null;
            }
            else if (type == "ROBOT")
            {
                _viewModel.SelectedFilterType = "Robot";
                _viewModel.SelectedGroupName = parts[1];
                _viewModel.SelectedStationName = parts[2];
                _viewModel.SelectedRobotName = parts[3];
            }

            UpdateBreadcrumbText();
            _viewModel.RefreshData();
            UpdateKPISummary();
        }

        private void SyncTreeViewSelection()
        {
            _isSyncingSelection = true;
            try
            {
                TreeNode targetNode = null;
                string targetTag = "";

                if (_viewModel.SelectedFilterType == "All")
                {
                    targetTag = "ALL";
                }
                else if (_viewModel.SelectedFilterType == "Group")
                {
                    targetTag = $"GROUP:{_viewModel.SelectedGroupName}";
                }
                else if (_viewModel.SelectedFilterType == "Station")
                {
                    targetTag = $"STATION:{_viewModel.SelectedGroupName}:{_viewModel.SelectedStationName}";
                }
                else if (_viewModel.SelectedFilterType == "Robot")
                {
                    targetTag = $"ROBOT:{_viewModel.SelectedGroupName}:{_viewModel.SelectedStationName}:{_viewModel.SelectedRobotName}";
                }

                targetNode = FindNodeByTag(tvStations.Nodes, targetTag);

                if (targetNode != null)
                {
                    tvStations.SelectedNode = targetNode;
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private TreeNode FindNodeByTag(TreeNodeCollection nodes, string tag)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag?.ToString() == tag)
                    return node;

                var childMatch = FindNodeByTag(node.Nodes, tag);
                if (childMatch != null)
                    return childMatch;
            }
            return null;
        }

        private void rbFilter_CheckedChanged(object sender, EventArgs e)
        {
            if (rbFilterAll.Checked)
            {
                _viewModel.CurrentFilter = DowntimeFilter.All;
            }
            else if (rbFilterAlarmed.Checked)
            {
                _viewModel.CurrentFilter = DowntimeFilter.Alarmed;
            }
            else if (rbFilterEmpty.Checked)
            {
                _viewModel.CurrentFilter = DowntimeFilter.Empty;
            }
        }

        private void dgvDowntimes_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
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

        private void dgvCycles_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
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

        private void dgvRawLogs_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
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

        private void tabMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabCtrl = sender as TabControl;
            if (tabCtrl == null) return;

            var g = e.Graphics;
            var tabBounds = tabCtrl.GetTabRect(e.Index);

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // Draw header background
            Color bg = isSelected ? Color.FromArgb(45, 45, 45) : Color.FromArgb(31, 31, 31);
            using (var bgBrush = new SolidBrush(bg))
            {
                g.FillRectangle(bgBrush, tabBounds);
            }

            // Draw header text
            string title = tabCtrl.TabPages[e.Index].Text;
            Color textCol = isSelected ? Color.FromArgb(0, 230, 118) : Color.FromArgb(160, 160, 160);
            using (var textBrush = new SolidBrush(textCol))
            using (var font = new Font(tabCtrl.Font.FontFamily, 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular))
            {
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(title, font, textBrush, tabBounds, stringFormat);
            }

            // Active tab green line decoration
            if (isSelected)
            {
                using (var pen = new Pen(Color.FromArgb(0, 230, 118), 3))
                {
                    g.DrawLine(pen, tabBounds.Left + 5, tabBounds.Bottom - 2, tabBounds.Right - 5, tabBounds.Bottom - 2);
                }
            }
        }

        private void SetupDowntimeDetailsPanel()
        {
            // Remove dgvDowntimes from tpDowntimes' controls temporarily
            tpDowntimes.Controls.Remove(dgvDowntimes);

            // Create SplitContainer
            var splitDowntimes = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 350,
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.FromArgb(224, 224, 224),
                SplitterWidth = 6
            };

            tpDowntimes.Controls.Add(splitDowntimes);

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
            var lblDetailsTitle = new Label
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
            dgvDowntimeDetails.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Başlangıç Zamanı", DataPropertyName = "StartTime", Width = 150 });
            dgvDowntimeDetails.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bitiş Zamanı", DataPropertyName = "EndTime", Width = 150 });
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
                detailForm.ShowDialog(this);
            }
        }

        private void SetupLeftPanelLayout()
        {
            var btnCollapseAll = new Button
            {
                Dock = DockStyle.Top,
                Height = 32,
                Text = "TÜMÜNÜ DARALT",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45), // #2D2D2D
                ForeColor = Color.FromArgb(224, 224, 224), // #E0E0E0
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCollapseAll.FlatAppearance.BorderSize = 1;
            btnCollapseAll.FlatAppearance.BorderColor = Color.FromArgb(85, 85, 85);
            btnCollapseAll.Click += (s, ev) =>
            {
                tvStations.CollapseAll();
                var root = FindNodeByTag(tvStations.Nodes, "ALL");
                if (root != null)
                {
                    root.Expand();
                }
                if (tvStations.Nodes.Count > 0)
                {
                    tvStations.SelectedNode = null;
                    tvStations.SelectedNode = tvStations.Nodes[0];
                    tvStations.Focus();
                }
            };

            var txtSearch = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.FromArgb(45, 45, 45), // #2D2D2D
                ForeColor = Color.FromArgb(224, 224, 224), // #E0E0E0
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Varlık Ara... (Örn: 9A3)"
            };
            txtSearch.TextChanged += (s, ev) =>
            {
                PopulateTreeView(txtSearch.Text);
                _viewModel.SearchText = txtSearch.Text;
                _viewModel.RefreshData();
            };

            pnlLeft.Controls.Add(txtSearch);
            pnlLeft.Controls.Add(btnCollapseAll);

            // Set DockStyles
            pnlFilters.Dock = DockStyle.Bottom;
            lblStationsTitle.Dock = DockStyle.Top;
            btnCollapseAll.Dock = DockStyle.Top;
            txtSearch.Dock = DockStyle.Top;
            tvStations.Dock = DockStyle.Fill;
            tvStations.Scrollable = true;

            // Bring to front in specific sequence to establish correct docking order
            pnlFilters.BringToFront();
            lblStationsTitle.BringToFront();
            btnCollapseAll.BringToFront();
            txtSearch.BringToFront();
            tvStations.BringToFront();
        }

        private void SetupBreadcrumbPanel()
        {
            pnlBreadcrumb = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(26, 36, 33), // Dark green-antrasit combo
                Padding = new Padding(1)
            };

            var pnlAccent = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Color.FromArgb(0, 230, 118) // Neon Green
            };
            pnlBreadcrumb.Controls.Add(pnlAccent);

            lblBreadcrumb = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(224, 224, 224),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Text = "Seçili Varlık: Tüm Varlıklar"
            };
            pnlBreadcrumb.Controls.Add(lblBreadcrumb);
        }

        private void SetupKPISummaryPanel()
        {
            var flpKPI = new FlowLayoutPanel
            {
                Dock = DockStyle.None,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            lblKPITotal = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 118), // Neon Green (#00E676)
                Margin = new Padding(5, 9, 5, 0),
                Text = "Genel Toplam: 0 |"
            };

            lblKPIAlarmed = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 74, 74), // Soft Red (#FF4A4A)
                Margin = new Padding(5, 9, 5, 0),
                Text = "Alarmlı: 0 |"
            };

            lblKPIUnalarmed = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(144, 202, 249), // Soft Blue (#90CAF9)
                Margin = new Padding(5, 9, 5, 0),
                Text = "Alarmsız: 0 ||"
            };

            lblSelectedTotal = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255), // Turquoise (#00E5FF)
                Margin = new Padding(5, 9, 5, 0),
                Text = "Seçili Toplam: 0 |"
            };

            lblSelectedAlarmed = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 145, 0), // Orange (#FF9100)
                Margin = new Padding(5, 9, 5, 0),
                Text = "Alarmlı: 0 |"
            };

            lblSelectedUnalarmed = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 214, 0), // Yellow (#FFD600)
                Margin = new Padding(5, 9, 5, 0),
                Text = "Alarmsız: 0"
            };

            var lblSeparator = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 120, 120), // Pale Gray
                Margin = new Padding(5, 9, 5, 0),
                Text = "  ║  "
            };

            flpKPI.Controls.Add(lblKPITotal);
            flpKPI.Controls.Add(lblKPIAlarmed);
            flpKPI.Controls.Add(lblKPIUnalarmed);
            flpKPI.Controls.Add(lblSeparator);
            flpKPI.Controls.Add(lblSelectedTotal);
            flpKPI.Controls.Add(lblSelectedAlarmed);
            flpKPI.Controls.Add(lblSelectedUnalarmed);

            // Configure statusStrip height and background color
            statusStrip.AutoSize = false;
            statusStrip.Height = 35;
            statusStrip.BackColor = Color.FromArgb(26, 26, 26); // #1A1A1A
            
            // Wrap flpKPI in a ToolStripControlHost and add to statusStrip.Items safely
            var host = new ToolStripControlHost(flpKPI)
            {
                Alignment = ToolStripItemAlignment.Right,
                BackColor = Color.Transparent
            };

            // Remove any existing ToolStripControlHost items to prevent duplicates
            for (int i = statusStrip.Items.Count - 1; i >= 0; i--)
            {
                if (statusStrip.Items[i] is ToolStripControlHost)
                {
                    statusStrip.Items.RemoveAt(i);
                }
            }

            statusStrip.Items.Add(host);

            // Configure splitMain.Panel2 layout and padding
            splitMain.Panel2.Padding = new Padding(0, 0, 0, 0);

            pnlBreadcrumb.Dock = DockStyle.Top;
            tabMain.Dock = DockStyle.Fill;

            // Ensure tabControl size mode allows auto-sizing of tab headers to prevent clipping
            tabMain.SizeMode = TabSizeMode.Normal;

            if (!splitMain.Panel2.Controls.Contains(pnlBreadcrumb))
            {
                splitMain.Panel2.Controls.Add(pnlBreadcrumb);
            }

            // Correct Z-order to prevent overlapping: Dock=Top control to back, Dock=Fill control to front
            pnlBreadcrumb.SendToBack();
            tabMain.BringToFront();
        }

        private void UpdateKPISummary()
        {
            if (lblKPITotal != null)
                lblKPITotal.Text = $"[FABRİKA GENELİ] Toplam: {_viewModel.GlobalTotalDowntimes} |";
            if (lblKPIAlarmed != null)
                lblKPIAlarmed.Text = $"Alarmlı: {_viewModel.GlobalAlarmedDowntimes} |";
            if (lblKPIUnalarmed != null)
                lblKPIUnalarmed.Text = $"Alarmsız: {_viewModel.GlobalUnalarmedDowntimes}";
            if (lblSelectedTotal != null)
                lblSelectedTotal.Text = $"[SEÇİLİ VARLIK] Toplam: {_viewModel.SelectedTotalDowntimes} |";
            if (lblSelectedAlarmed != null)
                lblSelectedAlarmed.Text = $"Alarmlı: {_viewModel.SelectedAlarmedDowntimes} |";
            if (lblSelectedUnalarmed != null)
                lblSelectedUnalarmed.Text = $"Alarmsız: {_viewModel.SelectedUnalarmedDowntimes}";
        }

        private void UpdateBreadcrumbText()
        {
            if (lblBreadcrumb == null) return;

            if (_viewModel.SelectedFilterType == "All")
            {
                lblBreadcrumb.Text = "Seçili Varlık: Tüm Varlıklar";
            }
            else if (_viewModel.SelectedFilterType == "Group")
            {
                lblBreadcrumb.Text = $"Seçili Varlık: {_viewModel.SelectedGroupName}";
            }
            else if (_viewModel.SelectedFilterType == "Station")
            {
                lblBreadcrumb.Text = $"Seçili Varlık: {_viewModel.SelectedGroupName} > {_viewModel.SelectedStationName}";
            }
            else if (_viewModel.SelectedFilterType == "Robot")
            {
                lblBreadcrumb.Text = $"Seçili Varlık: {_viewModel.SelectedGroupName} > {_viewModel.SelectedStationName} > {_viewModel.SelectedRobotName}";
            }
        }

        private void SetupGrafanaQueryWizardTab()
        {
            tpGrafanaWizard = new TabPage
            {
                Text = "Grafana Sorgu Sihirbazı",
                BackColor = Color.FromArgb(18, 18, 18)
            };

            var pnlInputs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 215,
                BackColor = Color.FromArgb(26, 26, 26),
                Padding = new Padding(20)
            };

            var lblDateTitle = new Label
            {
                Text = "Tarih Girişi (Veritabanı Eki):",
                Location = new Point(20, 15),
                Width = 250,
                Height = 18,
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            txtDate = new TextBox
            {
                Text = DateTime.Today.ToString("d_M_yyyy"),
                Location = new Point(20, 35),
                Width = 200,
                Height = 25,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };

            var lblChannelTitle = new Label
            {
                Text = "Channel Kodu:",
                Location = new Point(20, 70),
                Width = 250,
                Height = 18,
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            txtChannel = new TextBox
            {
                Text = "OPC71",
                Location = new Point(20, 90),
                Width = 200,
                Height = 25,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };

            var lblAssetTitle = new Label
            {
                Text = "Asset Adı:",
                Location = new Point(20, 125),
                Width = 250,
                Height = 18,
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            txtAsset = new TextBox
            {
                Text = "9A",
                Location = new Point(20, 145),
                Width = 200,
                Height = 25,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };

            chkAllAssets = new CheckBox
            {
                Text = "Tüm Varlıklardan Veri Al",
                Location = new Point(20, 180),
                Width = 250,
                Height = 20,
                ForeColor = Color.FromArgb(224, 224, 224),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };

            pnlInputs.Controls.Add(lblDateTitle);
            pnlInputs.Controls.Add(txtDate);
            pnlInputs.Controls.Add(lblChannelTitle);
            pnlInputs.Controls.Add(txtChannel);
            pnlInputs.Controls.Add(lblAssetTitle);
            pnlInputs.Controls.Add(txtAsset);
            pnlInputs.Controls.Add(chkAllAssets);

            var pnlOutput = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(20, 10, 20, 20)
            };

            var lblOutputTitle = new Label
            {
                Text = "ÜRETİLEN SQL SORGUSU",
                Dock = DockStyle.Top,
                Height = 25,
                ForeColor = Color.FromArgb(0, 230, 118),
                Font = new Font("Segoe UI Black", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            txtSqlQuery = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10f),
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            var pnlOutputActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.Transparent
            };

            btnCopyQuery = new Button
            {
                Text = "Sorguyu Kopyala",
                Dock = DockStyle.Left,
                Width = 150,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopyQuery.FlatAppearance.BorderSize = 1;
            btnCopyQuery.FlatAppearance.BorderColor = Color.FromArgb(85, 85, 85);
            btnCopyQuery.Click += btnCopyQuery_Click;

            lblCopyStatus = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(0, 230, 118),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };

            pnlOutputActions.Controls.Add(lblCopyStatus);
            pnlOutputActions.Controls.Add(btnCopyQuery);

            pnlOutput.Controls.Add(lblOutputTitle);
            pnlOutput.Controls.Add(pnlOutputActions);
            pnlOutput.Controls.Add(txtSqlQuery);

            lblOutputTitle.SendToBack();
            pnlOutputActions.SendToBack();
            txtSqlQuery.BringToFront();

            tpGrafanaWizard.Controls.Add(pnlInputs);
            tpGrafanaWizard.Controls.Add(pnlOutput);

            pnlInputs.SendToBack();
            pnlOutput.BringToFront();

            // Bind triggers for automatic query generation
            txtDate.TextChanged += (s, e) => GenerateSqlQuery();
            txtChannel.TextChanged += (s, e) => GenerateSqlQuery();
            txtAsset.TextChanged += (s, e) => GenerateSqlQuery();
            chkAllAssets.CheckedChanged += (s, e) =>
            {
                txtAsset.Enabled = !chkAllAssets.Checked;
                if (chkAllAssets.Checked)
                {
                    txtAsset.BackColor = Color.FromArgb(30, 30, 30);
                }
                else
                {
                    txtAsset.BackColor = Color.FromArgb(45, 45, 45);
                }
                GenerateSqlQuery();
            };

            // Generate initial query
            GenerateSqlQuery();

            // Add the Grafana Wizard Tab to tabMain as the 4th tab
            tabMain.TabPages.Add(tpGrafanaWizard);
        }

        private void GenerateSqlQuery()
        {
            if (txtDate == null || txtChannel == null || txtAsset == null || chkAllAssets == null || txtSqlQuery == null)
            {
                return;
            }

            string date = txtDate.Text.Trim();
            string channel = txtChannel.Text.Trim();
            string asset = chkAllAssets.Checked ? "" : txtAsset.Text.Trim();

            string query = @"SELECT * FROM kepware_opc_[TARİH]
WHERE ""TagName"" LIKE '%[CHANNEL]%[ASSET]%Outputs%Down%' OR ""TagName"" LIKE '%[CHANNEL]%[ASSET]%.-.Outputs.TransactionEnd%' OR ""TagName"" LIKE '%[CHANNEL]%[ASSET]%Alarm%'";

            query = query.Replace("[TARİH]", date);
            query = query.Replace("[CHANNEL]", channel);
            query = query.Replace("[ASSET]", asset);

            if (chkAllAssets.Checked)
            {
                query = query.Replace("%%", "%");
            }

            txtSqlQuery.Text = query;
        }

        private void btnCopyQuery_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSqlQuery.Text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(txtSqlQuery.Text);
                lblCopyStatus.Text = "Sorgu panoya kopyalandı!";

                var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                timer.Tick += (s, ev) =>
                {
                    lblCopyStatus.Text = "";
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Panoya kopyalama başarısız oldu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
