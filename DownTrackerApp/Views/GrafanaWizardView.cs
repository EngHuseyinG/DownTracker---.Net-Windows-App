using System;
using System.Drawing;
using System.Windows.Forms;
using DownTracker.ViewModels;

namespace DownTracker.Views
{
    public class GrafanaWizardView : UserControl
    {
        private readonly GrafanaWizardViewModel _viewModel;

        private TextBox txtDate;
        private TextBox txtChannel;
        private TextBox txtAsset;
        private CheckBox chkAllAssets;
        private RichTextBox txtSqlQuery;
        private Button btnCopyQuery;
        private Label lblCopyStatus;

        public GrafanaWizardViewModel ViewModel => _viewModel;

        public GrafanaWizardView()
        {
            _viewModel = new GrafanaWizardViewModel();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Dock = DockStyle.Fill;

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
            btnCopyQuery.Click += BtnCopyQuery_Click;

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

            this.Controls.Add(pnlInputs);
            this.Controls.Add(pnlOutput);

            pnlInputs.SendToBack();
            pnlOutput.BringToFront();

            // Set initial state from view model
            txtDate.Text = _viewModel.Date;
            txtChannel.Text = _viewModel.Channel;
            txtAsset.Text = _viewModel.Asset;
            chkAllAssets.Checked = _viewModel.AllAssets;
            txtSqlQuery.Text = _viewModel.SqlQuery;

            // Bind inputs to view model
            txtDate.TextChanged += (s, e) => _viewModel.Date = txtDate.Text;
            txtChannel.TextChanged += (s, e) => _viewModel.Channel = txtChannel.Text;
            txtAsset.TextChanged += (s, e) => _viewModel.Asset = txtAsset.Text;
            chkAllAssets.CheckedChanged += (s, e) =>
            {
                _viewModel.AllAssets = chkAllAssets.Checked;
                txtAsset.Enabled = !chkAllAssets.Checked;
                txtAsset.BackColor = chkAllAssets.Checked ? Color.FromArgb(30, 30, 30) : Color.FromArgb(45, 45, 45);
            };

            // Bind outputs
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.SqlQuery))
                {
                    txtSqlQuery.Text = _viewModel.SqlQuery;
                }
            };
        }

        private void BtnCopyQuery_Click(object sender, EventArgs e)
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
