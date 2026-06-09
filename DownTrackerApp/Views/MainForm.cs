using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DownTracker.Models;
using DownTracker.ViewModels;

namespace DownTracker.Views
{
    public partial class MainForm : Form
    {
        private readonly MainViewModel _viewModel;
        private bool _isSyncingSelection = false;
        private DownConditionsView _downConditionsView;
        private CycleAnalysisView _cycleAnalysisView;
        private RawLogsView _rawLogsView;
        private Label lblBreadcrumb;
        private Panel pnlBreadcrumb;
        private FlowLayoutPanel pnlFooter;
        private Label lblKPITotal, lblKPIAlarmed, lblKPIUnalarmed;
        private Label lblSelectedTotal, lblSelectedAlarmed, lblSelectedUnalarmed;

        // Grafana Query Wizard Controls
        private TabPage tpGrafanaWizard;
        private GrafanaWizardView _grafanaWizardView;

        // Loading Overlay Controls
        private Panel pnlLoadingOverlay;
        private ProgressBar pbLoading;
        private Label lblLoadingText;

        // Import Wizard Controls
        private TabPage tpImportWizard;
        private ImportWizardView _importWizardView;
        private ImageList _tabImageList;

        // Faulty Import Wizard Controls
        private TabPage tpFaultyImport;
        private FaultyImportView _faultyImportView;

        public MainForm()
        {
            InitializeComponent();
            this.Padding = new Padding(0, 12, 0, 0); // Üst sınırdan 12 piksellik asil bir boşluk açar
            _viewModel = new MainViewModel();

            // Initialize dynamic GDI+ tab ImageList
            _tabImageList = CreateImageList(16);
            tabMain.ImageList = _tabImageList;
            tabMain.Padding = new Point(18, 6); // Add padding to give icons spacing on the left

            // Evrensel kurumsal dile uygun tab başlığı güncellemeleri
            tpDowntimes.Text = "Down Conditions";
            tpDowntimes.ImageIndex = 0;
            tpCycles.Text = "Çevrim Analizi";
            tpCycles.ImageIndex = 1;
            tpRawLogs.Text = "Ham Veri İzleme";
            tpRawLogs.ImageIndex = 2;

            // Configure SplitContainer panels for minimalist look
            splitMain.Panel1MinSize = 150;
            splitMain.SplitterDistance = 180;

            // Enable Drag and Drop on the top panel and label
            pnlTop.AllowDrop = true;
            lblDragDrop.AllowDrop = true;
            pnlTop.Padding = new Padding(20, 15, 20, 10); // Sol: 20, Üst: 15, Sağ: 20, Alt: 10 piksel marjin payı

            pnlTop.BorderStyle = BorderStyle.None;
            pnlTop.Resize += (s, e) => pnlTop.Invalidate();
            pnlTop.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(0, 229, 255), 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlTop.Width - 1, pnlTop.Height - 1);
                }
            };

            pnlTop.DragEnter += LblDragDrop_DragEnter;
            pnlTop.DragLeave += LblDragDrop_DragLeave;
            pnlTop.DragDrop += LblDragDrop_DragDrop;
            pnlTop.Click += lblDragDrop_Click;
            lblFilePath.Click += lblDragDrop_Click;
            lblFilePath.Cursor = Cursors.Hand;

            lblDragDrop.DragEnter += LblDragDrop_DragEnter;
            lblDragDrop.DragLeave += LblDragDrop_DragLeave;
            lblDragDrop.DragDrop += LblDragDrop_DragDrop;

            var lblMainImportGuide = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 160, 160), // Gözü yormayan soluk gri/antrasit tonu
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "💡 İpucu: Bu uygulamanın ihtiyaç duyduğu hatasız ve tam uyumlu Grafana ham log çıktısını elde etmek için üst menüdeki 'Grafana Sorgu Sihirbazı' modülünü kullanabilirsiniz."
            };
            lblMainImportGuide.Cursor = Cursors.Hand;
            lblMainImportGuide.Click += lblDragDrop_Click;
            pnlTop.Controls.Add(lblMainImportGuide);

            // Stack controls inside pnlTop:
            lblFilePath.SendToBack();
            lblMainImportGuide.BringToFront();
            lblDragDrop.BringToFront();

            // "?" Kılavuz butonu — Neon Cyan dikey pill, pnlTop sağ kenarına sabitlenmiş
            var tipGuide = new ToolTip
            {
                InitialDelay = 400,
                ReshowDelay = 200,
                AutoPopDelay = 5000,
                ShowAlways = true
            };

            var btnHelp = new Button
            {
                Text = "?",
                Size = new Size(60, pnlTop.Height - 16),
                Location = new Point(pnlTop.Width - 72, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(18, 18, 24),
                ForeColor = Color.FromArgb(0, 229, 255),
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnHelp.FlatAppearance.BorderSize = 1;
            btnHelp.FlatAppearance.BorderColor = Color.FromArgb(0, 229, 255);
            btnHelp.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 229, 255);
            btnHelp.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 185, 210);

            tipGuide.SetToolTip(btnHelp, "Kullanım Kılavuzu");

            btnHelp.MouseEnter += (s, e) => btnHelp.ForeColor = Color.FromArgb(17, 17, 17);
            btnHelp.MouseLeave += (s, e) =>
            {
                btnHelp.BackColor = Color.FromArgb(18, 18, 24);
                btnHelp.ForeColor = Color.FromArgb(0, 229, 255);
            };

            btnHelp.Click += (s, e) =>
            {
                using (var guide = new GuideForm())
                    guide.ShowDialog(this);
            };

            pnlTop.Controls.Add(btnHelp);
            btnHelp.BringToFront();

            _downConditionsView = new DownConditionsView(_viewModel);
            tpDowntimes.Controls.Add(_downConditionsView);

            _cycleAnalysisView = new CycleAnalysisView(_viewModel);
            tpCycles.Controls.Add(_cycleAnalysisView);

            _rawLogsView = new RawLogsView(_viewModel);
            tpRawLogs.Controls.Add(_rawLogsView);

            SetupBindings();
            SetupBreadcrumbPanel();
            SetupKPISummaryPanel();
            SetupLeftPanelLayout();
            SetupGrafanaQueryWizardTab();
            SetupImportWizardTab();
            SetupFaultyImportTab();
            SetupLoadingOverlay();

            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.MaximizedBounds = Screen.PrimaryScreen.WorkingArea;
            this.WindowState = FormWindowState.Maximized;

            // Set initial state
            _viewModel.RefreshData();
            UpdateKPISummary();

            // Perform initial chronological sort
            _downConditionsView?.SortMainData("StartTime");
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                using (var guide = new GuideForm())
                {
                    guide.ShowDialog(this);
                }
                e.Handled = true;
            }
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

        }

        private void LblDragDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                pnlTop.BackColor = Color.FromArgb(45, 45, 45);
                lblDragDrop.ForeColor = Color.FromArgb(0, 229, 255);
            }
        }

        private void LblDragDrop_DragLeave(object sender, EventArgs e)
        {
            pnlTop.BackColor = Color.FromArgb(31, 31, 31);
        }

        private async void LblDragDrop_DragDrop(object sender, DragEventArgs e)
        {
            pnlTop.BackColor = Color.FromArgb(31, 31, 31);
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                await LoadFileAsync(files[0]);
            }
        }

        private async void lblDragDrop_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV Dosyaları (*.csv)|*.csv|Tüm Dosyalar (*.*)|*.*";
                openFileDialog.Title = "Duruş Log Dosyasını Seçin";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    await LoadFileAsync(openFileDialog.FileName);
                }
            }
        }

        private async Task LoadFileAsync(string filePath)
        {
            try
            {
                pnlLoadingOverlay.Visible = true;
                pnlLoadingOverlay.BringToFront();

                _viewModel.StatusText = "Endüstriyel loglar analiz ediliyor, lütfen bekleyin...";

                // Heavy loading and processing done in background thread
                var parseResult = await Task.Run(() =>
                {
                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException("Belirtilen dosya bulunamadı.", filePath);
                    }

                    var logs = ParsingEngine.ParseCsv(filePath);
                    if (logs.Count == 0)
                    {
                        throw new Exception("Log dosyasından geçerli veri satırları çözümlenemedi. Lütfen dosya içeriğini kontrol edin.");
                    }

                    ParsingEngine.ProcessData(logs, out var cycles, out var downtimes, out var robots, out var groups);
                    return new { logs, cycles, downtimes, robots, groups };
                });

                // Apply the processed data to the ViewModel on the UI thread
                _viewModel.ApplyProcessedData(filePath, parseResult.logs, parseResult.cycles, parseResult.downtimes, parseResult.robots, parseResult.groups);

                PopulateTreeView();

                // Collapse tree and expand only the root node on initial load
                tvStations.CollapseAll();
                if (tvStations.Nodes.Count > 0)
                {
                    tvStations.Nodes[0].Expand();
                }

                UpdateKPISummary();
                // Re-apply sorting on new file load
                if (_downConditionsView != null)
                {
                    _downConditionsView.SortMainData(_downConditionsView.CurrentSortColumn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sistem Hatası Yakalandı!\n\nMesaj: {ex.Message}\n\nHata Yeri: {ex.TargetSite}\n\nDetay: {ex.ToString()}", "Kök Hata Raporu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                pnlLoadingOverlay.Visible = false;
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
            // Re-apply sorting on tree node selection change
            if (_downConditionsView != null)
            {
                _downConditionsView.SortMainData(_downConditionsView.CurrentSortColumn);
            }
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
            // Re-apply sorting on filter change
            if (_downConditionsView != null)
            {
                _downConditionsView.SortMainData(_downConditionsView.CurrentSortColumn);
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
                int imageIndex = tabCtrl.TabPages[e.Index].ImageIndex;
                Image img = null;
                if (tabCtrl.ImageList != null && imageIndex >= 0 && imageIndex < tabCtrl.ImageList.Images.Count)
                {
                    img = tabCtrl.ImageList.Images[imageIndex];
                }

                if (img != null)
                {
                    int spacing = 8; // Increased spacing between icon and text from 6 to 8
                    float textWidth = g.MeasureString(title, font).Width;
                    float totalWidth = img.Width + spacing + textWidth;
                    float startX = tabBounds.Left + (tabBounds.Width - totalWidth) / 2f;
                    float imgY = tabBounds.Top + (tabBounds.Height - img.Height) / 2f;

                    g.DrawImage(img, startX, imgY);

                    var stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center
                    };
                    var textRect = new RectangleF(startX + img.Width + spacing, tabBounds.Top, textWidth + 10f, tabBounds.Height);
                    g.DrawString(title, font, textBrush, textRect, stringFormat);
                }
                else
                {
                    var stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(title, font, textBrush, tabBounds, stringFormat);
                }
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
                // Re-apply sorting on search text change
                if (_downConditionsView != null)
                {
                    _downConditionsView.SortMainData(_downConditionsView.CurrentSortColumn);
                }
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
            pnlFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(26, 26, 26),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(15, 12, 15, 5)
            };
            pnlFooter.HorizontalScroll.Enabled = false;
            pnlFooter.HorizontalScroll.Visible = false;
            pnlFooter.VerticalScroll.Enabled = false;
            pnlFooter.VerticalScroll.Visible = false;

            Color neonRed = Color.FromArgb(255, 0, 85);
            Color softOrange = Color.FromArgb(255, 145, 0);

            lblKPITotal = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(224, 224, 224),
                Margin = new Padding(5, 0, 5, 0),
                Text = "Genel - Toplam: 0 |"
            };

            lblKPIAlarmed = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = neonRed,
                Margin = new Padding(5, 0, 5, 0),
                Text = "Alarmlı: 0 |"
            };

            lblKPIUnalarmed = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = softOrange,
                Margin = new Padding(5, 0, 5, 0),
                Text = "Alarmsız: 0 |"
            };

            lblSelectedTotal = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(224, 224, 224),
                Margin = new Padding(25, 0, 5, 0),
                Text = "Seçili - Toplam: 0 |"
            };

            lblSelectedAlarmed = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = neonRed,
                Margin = new Padding(5, 0, 5, 0),
                Text = "Alarmlı: 0 |"
            };

            lblSelectedUnalarmed = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = softOrange,
                Margin = new Padding(5, 0, 5, 0),
                Text = "Alarmsız: 0"
            };

            pnlFooter.Controls.Add(lblKPITotal);
            pnlFooter.Controls.Add(lblKPIAlarmed);
            pnlFooter.Controls.Add(lblKPIUnalarmed);
            pnlFooter.Controls.Add(lblSelectedTotal);
            pnlFooter.Controls.Add(lblSelectedAlarmed);
            pnlFooter.Controls.Add(lblSelectedUnalarmed);

            if (!this.Controls.Contains(pnlFooter))
            {
                this.Controls.Add(pnlFooter);
            }

            // Configure statusStrip height and background color
            statusStrip.AutoSize = false;
            statusStrip.Height = 35;
            statusStrip.BackColor = Color.FromArgb(20, 20, 20);

            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            // Configure designed by signature in statusStrip
            var lblSpringSpacer = new ToolStripStatusLabel
            {
                Name = "lblSpringSpacer",
                Spring = true
            };

            var lblDesignerSignature = new ToolStripStatusLabel
            {
                Name = "lblDesignerSignature",
                Text = "Designed by Hüseyin Gürel",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 140, 140),
                Margin = new Padding(0, 0, 10, 0),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Remove any existing ToolStripStatusLabel spacer/signature items to prevent duplicates
            for (int i = statusStrip.Items.Count - 1; i >= 0; i--)
            {
                if (statusStrip.Items[i].Name == "lblDesignerSignature" ||
                    statusStrip.Items[i].Name == "lblSpringSpacer")
                {
                    statusStrip.Items.RemoveAt(i);
                }
            }

            statusStrip.Items.Add(lblSpringSpacer);
            statusStrip.Items.Add(lblDesignerSignature);

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

            // Correct Z-order to prevent overlapping: Dock=Bottom controls stack correctly
            pnlTop.SendToBack();       // pnlTop goes to the very top (outermost top)
            statusStrip.SendToBack();  // statusStrip goes to the very bottom (outermost bottom)
            pnlFooter.BringToFront();  // pnlFooter goes above statusStrip (innermost bottom)
            splitMain.BringToFront();  // splitMain goes to the front of all docked panels, filling the remaining center space
        }

        private void UpdateKPISummary()
        {
            double totalAlarmPercent = _viewModel.GlobalTotalDowntimes > 0
                ? ((double)_viewModel.GlobalAlarmedDowntimes / _viewModel.GlobalTotalDowntimes) * 100
                : 0.0;

            double totalUnalarmedPercent = _viewModel.GlobalTotalDowntimes > 0
                ? ((double)_viewModel.GlobalUnalarmedDowntimes / _viewModel.GlobalTotalDowntimes) * 100
                : 0.0;

            double selectedAlarmPercent = _viewModel.SelectedTotalDowntimes > 0
                ? ((double)_viewModel.SelectedAlarmedDowntimes / _viewModel.SelectedTotalDowntimes) * 100
                : 0.0;

            double selectedUnalarmedPercent = _viewModel.SelectedTotalDowntimes > 0
                ? ((double)_viewModel.SelectedUnalarmedDowntimes / _viewModel.SelectedTotalDowntimes) * 100
                : 0.0;

            if (lblKPITotal != null)
                lblKPITotal.Text = $"Genel - Toplam: {_viewModel.GlobalTotalDowntimes} |";
            if (lblKPIAlarmed != null)
                lblKPIAlarmed.Text = $"Alarmlı: {_viewModel.GlobalAlarmedDowntimes} ({totalAlarmPercent:F1}%) |";
            if (lblKPIUnalarmed != null)
                lblKPIUnalarmed.Text = $"Alarmsız: {_viewModel.GlobalUnalarmedDowntimes} ({totalUnalarmedPercent:F1}%) |";
            if (lblSelectedTotal != null)
                lblSelectedTotal.Text = $"Seçili - Toplam: {_viewModel.SelectedTotalDowntimes} |";
            if (lblSelectedAlarmed != null)
                lblSelectedAlarmed.Text = $"Alarmlı: {_viewModel.SelectedAlarmedDowntimes} ({selectedAlarmPercent:F1}%) |";
            if (lblSelectedUnalarmed != null)
                lblSelectedUnalarmed.Text = $"Alarmsız: {_viewModel.SelectedUnalarmedDowntimes} ({selectedUnalarmedPercent:F1}%)";
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
                ImageIndex = 3,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            _grafanaWizardView = new GrafanaWizardView();
            tpGrafanaWizard.Controls.Add(_grafanaWizardView);

            // Add the Grafana Wizard Tab to tabMain as the 4th tab
            tabMain.TabPages.Add(tpGrafanaWizard);
        }

        private void SetupLoadingOverlay()
        {
            pnlLoadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                BorderStyle = BorderStyle.None,
                Visible = false
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));

            var pnlCenterContent = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Anchor = AnchorStyles.None,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            pbLoading = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Width = 350,
                Height = 15,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 15)
            };

            lblLoadingText = new Label
            {
                Text = "Endüstriyel loglar analiz ediliyor, lütfen bekleyin...",
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.None
            };

            pnlCenterContent.Controls.Add(pbLoading);
            pnlCenterContent.Controls.Add(lblLoadingText);

            layout.Controls.Add(pnlCenterContent, 0, 1);
            pnlLoadingOverlay.Controls.Add(layout);
            this.Controls.Add(pnlLoadingOverlay);
        }

        public void SetLoadingOverlay(bool visible, string text = "")
        {
            if (visible)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    lblLoadingText.Text = text;
                }
                pnlLoadingOverlay.Visible = true;
                pnlLoadingOverlay.BringToFront();
            }
            else
            {
                pnlLoadingOverlay.Visible = false;
            }
        }

        private void SetupImportWizardTab()
        {
            tpImportWizard = new TabPage
            {
                Text = "P360 Import Sihirbazı",
                ImageIndex = 4,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            _importWizardView = new ImportWizardView(_viewModel);
            tpImportWizard.Controls.Add(_importWizardView);

            tabMain.TabPages.Add(tpImportWizard);
        }

        private void SetupFaultyImportTab()
        {
            tpFaultyImport = new TabPage
            {
                Text = "P360 Hatalı Import Sihirbazı",
                ImageIndex = 6,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            _faultyImportView = new FaultyImportView(_viewModel);
            tpFaultyImport.Controls.Add(_faultyImportView);

            tabMain.TabPages.Add(tpFaultyImport);
        }



        private ImageList CreateImageList(int size)
        {
            var list = new ImageList
            {
                ImageSize = new Size(size, size),
                ColorDepth = ColorDepth.Depth32Bit
            };

            // 0. Down Conditions (vertical bar graph)
            list.Images.Add(CreateIcon(size, g =>
            {
                using (var brush = new SolidBrush(Color.FromArgb(255, 0, 85)))
                {
                    float barWidth = size * 0.2f;
                    float gap = size * 0.1f;
                    float startX = size * 0.15f;
                    g.FillRectangle(brush, startX, size * 0.5f, barWidth, size * 0.4f);
                    g.FillRectangle(brush, startX + barWidth + gap, size * 0.2f, barWidth, size * 0.7f);
                    g.FillRectangle(brush, startX + 2 * (barWidth + gap), size * 0.35f, barWidth, size * 0.55f);
                }
            }));

            // 1. Çevrim Analizi (circular loop)
            list.Images.Add(CreateIcon(size, g =>
            {
                using (var pen = new Pen(Color.FromArgb(143, 170, 220), size * 0.1f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    float margin = size * 0.15f;
                    float rectSize = size - 2 * margin;
                    g.DrawArc(pen, margin, margin, rectSize, rectSize, -60, 280);
                    using (var brush = new SolidBrush(Color.FromArgb(143, 170, 220)))
                    {
                        float arrowSize = size * 0.2f;
                        PointF[] arrowPoints = {
                            new PointF(size * 0.55f, margin - arrowSize * 0.5f),
                            new PointF(size * 0.75f, margin + arrowSize * 0.5f),
                            new PointF(size * 0.45f, margin + arrowSize * 1.5f)
                        };
                        g.FillPolygon(brush, arrowPoints);
                    }
                }
            }));

            // 2. Ham Veri İzleme (spreadsheet grid)
            list.Images.Add(CreateIcon(size, g =>
            {
                using (var pen = new Pen(Color.FromArgb(127, 127, 127), size * 0.08f))
                {
                    float margin = size * 0.15f;
                    float rectSize = size - 2 * margin;
                    g.DrawRectangle(pen, margin, margin, rectSize, rectSize);
                    float mid = size * 0.5f;
                    g.DrawLine(pen, margin, mid, size - margin, mid);
                    g.DrawLine(pen, mid, margin, mid, size - margin);
                }
            }));

            // 3. Sorgu Sihirbazı (terminal script prompt: >_ )
            list.Images.Add(CreateIcon(size, g =>
            {
                using (var pen = new Pen(Color.FromArgb(0, 230, 118), size * 0.12f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    PointF[] points = {
                        new PointF(size * 0.2f, size * 0.25f),
                        new PointF(size * 0.5f, size * 0.5f),
                        new PointF(size * 0.2f, size * 0.75f)
                    };
                    g.DrawLines(pen, points);
                    g.DrawLine(pen, size * 0.6f, size * 0.75f, size * 0.85f, size * 0.75f);
                }
            }));

            // 4. P360 Import Sihirbazı (arrow pointing into page)
            list.Images.Add(CreateIcon(size, g =>
            {
                using (var pen = new Pen(Color.FromArgb(255, 145, 0), size * 0.08f))
                {
                    float margin = size * 0.15f;
                    float w = size * 0.7f;
                    float h = size * 0.7f;
                    g.DrawRectangle(pen, margin, margin, w, h);
                    using (var brush = new SolidBrush(Color.FromArgb(255, 145, 0)))
                    {
                        float mid = size * 0.5f;
                        float arrowY = size * 0.45f;
                        g.FillPolygon(brush, new[] {
                            new PointF(mid, size * 0.7f),
                            new PointF(mid - size * 0.18f, arrowY),
                            new PointF(mid + size * 0.18f, arrowY)
                        });
                        g.DrawLine(pen, mid, size * 0.25f, mid, arrowY);
                    }
                }
            }));

            // 5. Nasıl Çalışır (info circle "i")
            list.Images.Add(CreateIcon(size, g =>
            {
                using (var pen = new Pen(Color.FromArgb(0, 229, 255), size * 0.08f))
                {
                    float margin = size * 0.15f;
                    float rectSize = size - 2 * margin;
                    g.DrawEllipse(pen, margin, margin, rectSize, rectSize);
                    using (var brush = new SolidBrush(Color.FromArgb(0, 229, 255)))
                    {
                        float mid = size * 0.5f;
                        g.FillEllipse(brush, mid - size * 0.05f, size * 0.3f, size * 0.1f, size * 0.1f);
                        g.FillRectangle(brush, mid - size * 0.04f, size * 0.45f, size * 0.08f, size * 0.25f);
                    }
                }
            }));

            // 6. Hatalı Import Sihirbazı (red cross "X")
            list.Images.Add(CreateIcon(size, g =>
            {
                using (var pen = new Pen(Color.FromArgb(255, 74, 74), size * 0.12f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    float margin = size * 0.25f;
                    g.DrawLine(pen, margin, margin, size - margin, size - margin);
                    g.DrawLine(pen, size - margin, margin, margin, size - margin);
                }
            }));

            return list;
        }

        private Bitmap CreateIcon(int size, Action<Graphics> drawAction)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                drawAction(g);
            }
            return bmp;
        }
    }
}
