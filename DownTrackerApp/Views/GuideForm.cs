using System;
using System.Drawing;
using System.Windows.Forms;

namespace DownTracker.Views
{
    public class GuideForm : Form
    {
        private Panel pnlGuideTabs;
        private Panel pnlGuideContent;
        private Button btnHowItWorks;
        private Button btnDownConditions;
        private Button btnProdCycles;
        private Button btnRawData;
        private Button btnGrafanaWizard;
        private Button btnImportWizard;
        private Button btnFaultyImportGuide;
        private Button btnAboutProduct;
        private ImageList _guideImageList;

        public GuideForm()
        {
            InitializeComponent();

            // Wire Load event to trigger the first click
            this.Load += (s, e) => btnHowItWorks.PerformClick();
        }

        private void InitializeComponent()
        {
            this.Text = "Teknik Kılavuz & Uygulama Rehberi";
            this.Size = new Size(1020, 580);
            this.MinimumSize = new Size(800, 500);
            this.BackColor = Color.FromArgb(15, 15, 15);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowIcon = false;

            // Generate ImageList for guide
            _guideImageList = CreateImageList(24);

            // Left Tab Panel
            pnlGuideTabs = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = Color.FromArgb(26, 26, 26)
            };

            // Right Content Panel
            pnlGuideContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                Padding = new Padding(25)
            };

            // 1. Nasıl Çalışır
            btnHowItWorks = CreateTabButton("Nasıl Çalışır / Proses Açıklaması", 0, 7);
            btnHowItWorks.Click += (s, e) => SelectTab(btnHowItWorks, () => ShowCard(
                "Nasıl Çalışır / Proses Açıklaması",
                "💡 UYGULAMA AMACI:\n" +
                "Sahadaki Kepware veya Grafana kaynaklı ham alarm ve duruş zaman serisi loglarını alıp, her duruşun gerçek kök nedenini otomatik olarak tespit eden endüstriyel bir diagnostic gateway uygulamasıdır.\n\n" +
                "🛠️ ÇEKİRDEK PROSES AKIŞI (4 Aşama):\n" +
                "1. Veri Toplama: Kepware veya Grafana üzerinden alınan ham zaman serisi logları asenkron motor tarafından parse edilir.\n" +
                "2. Çevrim İzolasyonu: İstasyonların TransactionEnd = True sinyalleri yakalanarak iki üretim arasındaki tam çevrim pencereleri matematiksel olarak bölünür.\n" +
                "3. Kök Neden Taraması: Duruşun gerçek kök nedeni, sadece o arızanın içinde gerçekleştiği iki TransactionEnd sınırı içerisinde taranarak süzülür.\n" +
                "4. Diagnostic Çıktı: Ayıklanan anomali ve kök neden alarmları operatör paneline infografik kartlar olarak basılır.\n\n" +
                "⚡ ARKA PLAN MİMARİSİ:\n" +
                "Tüm veri işleme işlemleri UI Thread'i kilitlemeden arka planda (Task.Run) yürütülür. Çevrim bazlı alarm tarama algoritması, saniye farklarından bağımsız olarak sadece meşru üretim pencereleri içinde çalışır.",
                Color.FromArgb(0, 229, 255), 7
            ));

            // 2. Down Conditions
            btnDownConditions = CreateTabButton("Down Conditions Ekranı", 55, 0);
            btnDownConditions.Click += (s, e) => SelectTab(btnDownConditions, () => ShowCard(
                "Down Conditions Ekranı",
                "💡 MODÜLÜN AMACI:\n" +
                "Sahadan gelen ham kronolojik duruş kartlarının listelendiği ve her duruşun kök neden alarmıyla eşleştirildiği ana diagnostic odasıdır.\n\n" +
                "🛠️ KULLANIM KILAVUZU:\n" +
                "• Her duruş kartının üstünde, arızanın hattan çaldığı süre dikey kırmızı bir zaman şeridi olarak görsel çizilir.\n" +
                "• Tek Tıklama (Click): Bir duruş kartına tıklayın, üst GDI+ çizelgesinde o arızaya ait kök neden alarmı neon pembe renkte parlayarak kendini gösterir.\n" +
                "• Çift Tıklama (Double-Click): Duruş kartına çift tıklayın, 'Mikro-Duruş Detay Ekranı' açılır.\n" +
                "• Hover (Fare Gezintisi): Zaman çizgisi üzerinde fareyle gezinerek anlık diagnostic durum doğrulaması yapılabilir.\n\n" +
                "⚡ MİKRO-DURUŞ DETAY EKRANI:\n" +
                "Duruşun gerçekleştiği zaman dilimine ait tüm Kepware sinyal ve alarm akışları milisaniyelik GDI+ zaman çizelgeleri ve grafik pencereleriyle görsel olarak listelenir.",
                Color.FromArgb(255, 0, 85), 0
            ));

            // 3. Üretim Çevrimleri
            btnProdCycles = CreateTabButton("Çevrim Analizi Ekranı", 110, 1);
            btnProdCycles.Click += (s, e) => SelectTab(btnProdCycles, () => ShowCard(
                "Çevrim Analizi Ekranı",
                "💡 MODÜLÜN AMACI:\n" +
                "Hattın meşru üretim periyotlarını ve çevrim sürelerini listeleyerek, hangi çevrimin standart sürenin (Cycle Time) üzerine çıktığını yakalamanızı sağlar.\n\n" +
                "🛠️ KULLANIM KILAVUZU:\n" +
                "• Her satır, iki TransactionEnd = True sinyali arasındaki tek bir üretim çevrimini temsil eder.\n" +
                "• Çevrim süresi standartın üzerindeyse ilgili satır vurgulanarak dikkat çekilir.\n" +
                "• Sol taraftaki varlık ağacı (Asset Tree) ile %100 senkronize çalışır; istasyon seçimi anında listeyi filtreler.\n\n" +
                "⚡ ARKA PLAN MİMARİSİ:\n" +
                "TransactionEnd sinyalleri matematiksel olarak bölünerek her çevrim penceresi izole edilir. Hesaplama arka planda asenkron yürütülür ve sonuçlar anlık olarak tabloya yansıtılır.",
                Color.FromArgb(143, 170, 220), 1
            ));

            // 4. Ham Veri
            btnRawData = CreateTabButton("Ham Veri Ekranı", 165, 2);
            btnRawData.Click += (s, e) => SelectTab(btnRawData, () => ShowCard(
                "Ham Veri Ekranı",
                "💡 MODÜLÜN AMACI:\n" +
                "Filtreleme ve bölme algoritmalarına uğramamış, zaman damgalı saf Kepware loglarının serbest listesidir. Sahanın ham sinyallerini doğrudan görmek ve arka plan haritalama motorunu manuel doğrulamak için kullanılır.\n\n" +
                "🛠️ KULLANIM KILAVUZU:\n" +
                "• Tüm veriler kronolojik sırayla listelenir; hiçbir algoritmik süzme uygulanmaz.\n" +
                "• Sol varlık ağacından istasyon seçerek ilgili sinyallere hızla ulaşabilirsiniz.\n" +
                "• Herhangi bir veri anomalisi gördüğünüzde, 'Down Conditions' ekranıyla çapraz doğrulama yapabilirsiniz.\n\n" +
                "⚡ NE ZAMAN KULLANILIR?\n" +
                "Otomatik algoritmaların sonuçlarından şüphe duyduğunuzda veya sahadaki gerçek sinyal akışını gözünüzle doğrulamak istediğinizde bu ekrana başvurun.",
                Color.FromArgb(127, 127, 127), 2
            ));

            // 5. Grafana Sorgu Sihirbazı
            btnGrafanaWizard = CreateTabButton("Grafana Sorgu Sihirbazı Ekranı", 220, 3);
            btnGrafanaWizard.Click += (s, e) => SelectTab(btnGrafanaWizard, () => ShowCard(
                "Grafana Sorgu Sihirbazı Ekranı",
                "💡 MODÜLÜN AMACI:\n" +
                "Grafana panelinden hatasız ve doğru formatta ham veri çekebilmeniz için gereken optimize edilmiş SQL sorgularını otonom olarak üretir.\n\n" +
                "🛠️ PROSES AKIŞI (2 Adım):\n" +
                "1. Tarih aralığını ve istasyonu seçin; sistem otomatik olarak optimize SQL sorgusunu üretir.\n" +
                "2. Üretilen sorguyu kopyalayın, Grafana paneline yapıştırın ve 'Download CSV' seçeneğini seçmeden dosyayı ham olarak indirin.\n\n" +
                "⚡ ÖNEMLİ UYARI:\n" +
                "Grafana'dan veri indirirken 'Download CSV' seçeneğini kesinlikle kullanmayın. Bu seçenek veri yapısını bozabilir ve uygulamanın parse motoruyla uyumsuzluk yaratabilir. Dosya her zaman ham (raw) olarak indirilmelidir.",
                Color.FromArgb(0, 230, 118), 3
            ));

            // 6. Import Sihirbazı
            btnImportWizard = CreateTabButton("P360 Import Sihirbazı Ekranı", 275, 4);
            btnImportWizard.Click += (s, e) => SelectTab(btnImportWizard, () => ShowCard(
                "P360 Import Sihirbazı Ekranı",
                "💡 MODÜLÜN AMACI:\n" +
                "Sahadaki çok sütunlu ham alarm listelerini, Platform 360 (P360) platformunun doğrudan kabul edeceği standart şablona dönüştürür.\n\n" +
                "🛠️ PROSES AKIŞI (Sıfır Buton, %100 Otonom):\n" +
                "1. Sürükleme alanına ham alarm Excel dosyanızı bırakın.\n" +
                "2. Sistem dosyayı aldığı an otomatik olarak P360 formatına dönüştürür ve SaveFileDialog ile size teslim eder.\n\n" +
                "⚡ ARKA PLAN MİMARİSİ:\n" +
                "Çıktı Excel dosyasının sütun genişlikleri otomatik ayarlanır, başlıklar kurumsal renk kodlarıyla mühürlenir. Code (F Sütunu) alanına statik veri yazılmaz; E sütunundaki kategoriye göre dinamik =IF() Excel formülleri canlı olarak hücrelere gömülür.",
                Color.FromArgb(255, 145, 0), 4
            ));

            // 7. P360 Hatalı Import Sihirbazı Kılavuzu
            btnFaultyImportGuide = CreateTabButton("P360 Hatalı Import Sihirbazı", 330, 6);
            btnFaultyImportGuide.Click += (s, e) => SelectTab(btnFaultyImportGuide, () => ShowCard(
                "P360 Hatalı Import Sihirbazı",
                "💡 MODÜLÜN AMACI:\n" +
                "P360 sistemine alarm listesi import edilirken, sistemsel veya senkronizasyon kaynaklı nedenlerle içeri alınamayan (reddedilen) alarmların hızlıca ayıklanması ve 'Nihai Kurtarma Paketi' oluşturulmasıdır.\n\n" +
                "🛠️ PROSES AKIŞI (Sıfır Buton, %100 Otonom):\n" +
                "1. İlk Sürükleme Alanına (Turkuaz): P360 sistemine daha önce yüklemeyi denediğiniz ve içinden hatalıların ayıklanmasını istediğiniz ESKİ ALARM EXCEL paketini bırakın.\n" +
                "2. İkinci Sürükleme Alanına (Kırmızı): Grafana panelinden çektiğiniz ve sisteme girilemeyen hatalı ham arıza loglarını barındıran CSV dosyasını bırakın.\n\n" +
                "⚡ ARKA PLAN MİMARİSİ:\n" +
                "Sistem Grafana dosyasını aldığı an hiçbir tıklama beklemeden otomatik tetiklenir. Yüz binlerce satırlık veri, hafızada 'HashSet' veri kümesi olarak yüksek hızlı indekslemeye tabi tutulur. Eski paket içerisindeki 'FULLTAGNAME' sütunu (680px) ile Grafana 'SensorName' sütunu (880px) milisaniyeler içinde kesiştirilerek sadece yüklenemeyen alarmlardan oluşan yeni bir P360 şablonu üretilir ve SaveFileDialog ile operatöre teslim edilir.",
                Color.FromArgb(255, 74, 74), 6
            ));

            // 8. Uygulama Hakkında / Künye
            btnAboutProduct = CreateTabButton("Uygulama Hakkında / Künye", 385, 5);
            btnAboutProduct.Click += (s, e) => SelectTab(btnAboutProduct, () => ShowCard(
                "Uygulama Hakkında / Künye",
                "• [Uygulama Adı]: DownTracker\n\n" +
                "• [Mevcut Sürüm]: v1.0\n\n" +
                "• [Yayın Tarihi (Release Date)]: 06.06.2026\n\n" +
                "• [Mühendislik ve Geliştirme Künyesi]: Developed by Hüseyin Gürel\n\n" +
                "--------------------------------------------------\n\n" +
                "💡 Sürüm Notu: Bu yazılım, endüstriyel sahadaki asenkron alarm ve duruş zaman serisi loglarını nesne izolasyon algoritmalarıyla süzerek kök neden analizi yapan bağımsız (Self-Contained) bir diagnostic gateway ürünüdür.",
                Color.FromArgb(0, 229, 255), 5
            ));

            pnlGuideTabs.Controls.Add(btnAboutProduct);
            pnlGuideTabs.Controls.Add(btnFaultyImportGuide);
            pnlGuideTabs.Controls.Add(btnImportWizard);
            pnlGuideTabs.Controls.Add(btnGrafanaWizard);
            pnlGuideTabs.Controls.Add(btnRawData);
            pnlGuideTabs.Controls.Add(btnProdCycles);
            pnlGuideTabs.Controls.Add(btnDownConditions);
            pnlGuideTabs.Controls.Add(btnHowItWorks);

            this.Controls.Add(pnlGuideContent);
            this.Controls.Add(pnlGuideTabs);
        }

        private Button CreateTabButton(string text, int top, int imageIndex)
        {
            var btn = new Button
            {
                Text = "  " + text,
                Location = new Point(0, top),
                Width = 280,
                Height = 55,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(26, 26, 26),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ImageList = _guideImageList,
                ImageIndex = imageIndex,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 40);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 33, 33);
            return btn;
        }

        private void SelectTab(Button activeBtn, Action loadContent)
        {
            foreach (Control ctrl in pnlGuideTabs.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.BackColor = Color.FromArgb(26, 26, 26);
                    btn.ForeColor = Color.FromArgb(180, 180, 180);
                }
            }
            activeBtn.BackColor = Color.FromArgb(45, 45, 45);
            activeBtn.ForeColor = Color.White;
            loadContent();
        }

        private void ShowCard(string title, string contentText, Color titleColor, int imageIndex = -1)
        {
            pnlGuideContent.Controls.Clear();

            // Main Card Panel
            var pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 22, 22),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(30)
            };

            var lblTitle = new Label
            {
                Text = "        " + title,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = titleColor,
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleLeft,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            if (imageIndex >= 0 && imageIndex < _guideImageList.Images.Count)
                lblTitle.Image = _guideImageList.Images[imageIndex];

            var lblContent = new Label
            {
                Text = contentText,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 220),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(0, 15, 0, 0),
                AutoSize = false
            };

            pnlCard.Controls.Add(lblContent);
            pnlCard.Controls.Add(lblTitle);

            pnlGuideContent.Controls.Add(pnlCard);
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

            // 6. P360 Hatalı Import Sihirbazı (X mark)
            list.Images.Add(CreateIcon(size, g =>
            {
                using (var pen = new Pen(Color.FromArgb(255, 74, 74), size * 0.12f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    float margin = size * 0.2f;
                    g.DrawLine(pen, margin, margin, size - margin, size - margin);
                    g.DrawLine(pen, size - margin, margin, margin, size - margin);
                }
            }));

            // 7. Nasıl Çalışır (büyüteç / analiz)
            list.Images.Add(CreateIcon(size, g =>
            {
                using (var pen = new Pen(Color.FromArgb(0, 229, 255), size * 0.1f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    // Lens dairesi
                    float lensX = size * 0.18f;
                    float lensY = size * 0.15f;
                    float lensDiam = size * 0.48f;
                    g.DrawEllipse(pen, lensX, lensY, lensDiam, lensDiam);
                    // Sap çizgisi
                    g.DrawLine(pen, lensX + lensDiam * 0.85f, lensY + lensDiam * 0.85f,
                                    size * 0.85f, size * 0.85f);
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
