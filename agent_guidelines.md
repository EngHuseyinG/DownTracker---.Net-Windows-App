# 📘 DownTracker App - Geliştirici ve Agent Kılavuzu (Nihai Sürüm)

## 🚨 1. EN YÜKSEK ÖNCELİKLİ KURAL: TOKEN TASARRUFU VE METODOLOJİ
- **Nokta Atışı Kodlama:** Agent, yapacağı kod değişikliklerinde projenin tüm dosyalarını veya koca metotları baştan sona ekrana yazarak token israfı yapamaz. Sadece değişen satırları, ilgili metot bloklarını veya eklenecek/silinecek kod parçalarını net diff veya parça kod blokları halinde vermelidir.
- **Laf Kalabalığından Kaçınma:** Yanıtlar teknik olarak yoğun, doğrudan amaca yönelik, scannable (hızlı taranabilir) ve kısa olmalıdır. Uzun teorik açıklamalar yerine doğrudan aksiyona geçilmelidir.

## 🏗️ 2. MİMARİ YAPI VE GEÇMİŞ GELİŞTİRMELER
- **Platform & Desen:** .NET Windows Forms (WinForms) platformu üzerinde MVVM (Model-View-ViewModel) tasarım deseni. Klasör yapısı: `Models`, `ViewModels`, `Views`, `Config`.
- **Excel/CSV Bağımsız Grafana Sorgu Motoru:** `GenerateSqlQuery()` metodu, hafızada yüklü bir veri veya sol ağaçta seçili bir asset olmasa bile çalışabilecek şekilde saf string manipülasyonu ile kurgulanmıştır. Giriş alanlarının (`txtDate`, `txtChannel`, `txtAsset`, `chkAllAssets`) anlık olaylarına (`TextChanged` / `CheckedChanged`) doğrudan bağlıdır. Manuel butonlar (`btnGenerateQuery`) tamamen kaldırılmıştır, bu otomatik akış kesinlikle bozulmamalıdır.
- **Minimalist Sol Panel ve Esnek Sağ Panel:** Sol taraftaki istasyon/robot ağacının genişliği (`splitMain.SplitterDistance`) endüstriyel standartlar gereği `180px` (min: `150px`) olarak sabitlenmiştir. Sağ taraftaki `tabMain` ise `Dock = DockStyle.Fill` olarak tüm alanı kapmakta, üst panellerle çakışmaması için `splitMain.Panel2.Padding` değeri `(0, 0, 0, 0)` olarak sıfırlanmış ve hiyerarşi `pnlBreadcrumb` ile dikeyde hizalanmıştır.
- **Üretim Çevrimleri Grid Standardı (dgvProductionCycles):** Üretim Çevrimleri listesinde de tıpkı Down Conditions listesinde olduğu gibi en sol tarafta (0. indeks) meşru 'Varlık Adı' (DataPropertyName: StationName veya AssetName) sütunu yer alır. Bu sayede iki ana tablonun görsel hiyerarşisi ve veri takibi sahadaki çapraz kontaminasyonu önleyecek şekilde eşitlenmiştir.

## 🐛 3. ÇÖZÜLEN KRİTİK HATALAR VE ASLA YAPILMAYACAKLAR
- **`Controls.Clear()` Yasağı (Disposed Koruması):** Sağ panel kontrol listesini yenilemek amacıyla asla `Controls.Clear()` çağrısı yapılamaz! Bu çağrı, tasarım anında (Design-time) oluşturulan `tabMain` nesnesini hafızadan tamamen silerek (disposed) uygulamanın çökmesine ve sekmelerin kaybolmasına neden olur. Kontroller hiyerarşik eklenmeli ve `BringToFront()` / `SendToBack()` ile yönetilmelidir.
- **TreeView Odaklanma ve CollapseAll Kilidi:** Ağaç yapısı daraltıldıktan (`CollapseAll`) sonra WinForms'un durum makinesinin kilitlenmesini ve 'Tüm Varlıklar' filtresinin tetiklenmemesini önlemek için seçim önce `null` yapılmalı, ardından manuel olarak kök düğüme eşitlenmeli ve odaklanılmalıdır:
  ```csharp
  tvStations.SelectedNode = null;
  tvStations.SelectedNode = tvStations.Nodes[0];
  tvStations.Focus();
  ```
- **İlk Yükleme Düzeni:** CSV/Excel dosyası sisteme ilk yüklendiğinde, ağaç yapısı karmaşayı önlemek için `CollapseAll()` edilmeli ve sadece en üstteki kök düğüm (`Nodes[0].Expand()`) genişletilmelidir. Dinamik arama esnasındaki `ExpandAll()` mekanizması ise korunmalıdır.

## 📊 4. ALT BAR KPI GÖSTERGE STANDARTLARI
- **ToolStripControlHost Entegrasyonu:** En alttaki `StatusStrip` içerisinde yer alan 6 adet KPI etiketini barındıran `flpKPI` FlowLayoutPanel nesnesi, WinForms mimarisine tam uyum için `ToolStripControlHost` ile sarmalanarak sağa dayalı (`Dock = Right`) yerleştirilmiştir.
- **Görsel Ayrıştırma:** Fabrika geneli veriler ile robota özel anlık verilerin karışmaması için araya soluk gri renkte (`Color.FromArgb(120, 120, 120)`) dikey çift-çizgi `  ║  ` ayraç etiketi yerleştirilmiştir. Şablonlar her zaman şu formatta güncellenmelidir:
  `[FABRİKA GENELİ] Toplam: X | Alarmlı: Y | Alarmsız: Z ║ [SEÇİLİ VARLIK] Toplam: A | Alarmlı: B | Alarmsız: C`

## 🏗️ 5. ARİZA DETAY VE ZAMAN ÇİZELGESİ (FORM_DOWNTIME_INFO) MİMARİSİ
- **🛑 DATA GRID VIEW VE WINDOWS ODAK (FOCUS) YASAĞI:**
  - **Kök Sorun (Zeytin Yeşili Problemi):** Pop-up pencerelerde DataGridView kullanıldığında, Windows tema motoru (uxtheme.dll) aktif odaklanan kontrol satırlarına kendi varsayılan Focused/Selected State rengini (donuk zeytin yeşili/kirli sarı) zorla dayatır. Bu durum C# düzeyindeki CellFormatting veya RowPrePaint özellik atamalarını tamamen bypass ederek arayüzün kilitlenmesine neden olur.
  - **Nihai Çözüm (Tablosuz Kart Mimarisi):** Detay ekranlarında DataGridView kullanımı tamamen yasaklanmıştır. Bunun yerine, işletim sistemi tema müdahalelerinden %100 izole, runtime kod tabanlı (Pure Code-Behind) FlowLayoutPanel ve Dinamik Kart (Custom Panel Cards) mimarisi getirilmiştir. Kurallar tamamen bizim GDI+ fırçalarımızın kontrolündedir.
- **🎨 GÖRSEL SENKRONİZASYON VE RENK ANAYASASI:**
  - **Çizelge Taban Çubuğu (Zemin):** TransactionEnd sinyalinin temsil ettiği tüm üretim çevrimi antrasit gri (#2D2D2D) şerit olarak çizilir. Sol ve sağ uç sınırlarına Color.White ve 8pt Segoe UI ile "TransactionEnd (Başlangıç)" ve "TransactionEnd (Bitiş)" yazılır.
  - **Default (Seçilmeyen) Kartlar ve Çubuklar:** Alarm veya duruş segmenti ayrımı yapılmaksızın, seçilmeyen tüm alt kartlar ve üst çizelgedeki arıza alanları milisaniyelik tam oranlamayla (mapping) Net Duruş Kırmızısı (Color.FromArgb(255, 74, 74)) olarak boyanır. Metinler koyu antrasit (#141414) yapılarak kontrast korunur.
  - **Seçilen Kart ve Parlama (Cross-Highlighting):** Kullanıcı listeden bir karta tıkladığında, _selectedSegmentIndex kontrolü en üst öncelikli elenir. Seçilen kart ve üst çizelgedeki izdüşümü aynı milisaniyede Canlı Neon Pembe (Color.FromArgb(255, 0, 85)) rengine bürünerek kusursuz bir çift yönlü diagnostic reaksiyon verir.
- **📐 MİLİMETRİK SÜTUN HİZALAMA STANDARTLARI:**
  - Tablosuz kart listesinde hizalamanın kaymaması için formun tepesinde statik bir başlık satırı (pnlHeaderRow) bulunur. Başlık ve alt kartların içindeki etiket genişlikleri dikey eksende milimetrik olarak şu ölçülerde sabitlenmiştir:
    - Başlangıç Zamanı: 140px
    - Bitiş Zamanı: 140px
    - Süre (ms/s): 70px
    - Alarm / Durum Açıklaması: 430px
    - Sol Padding Payı: 25px (FlowLayoutPanel scrollbar çıktığında taşmayı ve kırpılmayı önlemek amacıyla bırakılmıştır).
- **🔀 HOVER & CLICK ETKİLEŞİM ROLLERİNİN KESİN AYRIMI (HYBRID UX):**
  - **Merkezi Sinyal Detay Paneli (`pnlSignalSummary` & `lblCentralSignalInfo`):**
    - Bu panel YALNIZCA VE SADECE zaman çizelgesi üzerindeki MouseMove (Hover) hareketlerine endekslidir.
    - Fare dikey alarm çizgilerinin (3px kalınlığında olanlar) veya kırmızı duruş şeritlerinin üzerine geldiğinde anlık Kepware verisini bu panelde gösterir.
    - Fare boşluğa çıktığı an panel anında temizlenerek varsayılan yönlendirme metnine (`"İncelemek istediğiniz arıza veya alarm çizgisinin üzerine gelin..."`) geri döner.
  - **Alt Kart Tıklama İzolasyonu:**
    - Alttaki dinamik kart listesinde yapılan tıklama (Click) olayları, tepe panelindeki metni kesinlikle ama kesinlikle değiştiremez veya etkileyemez.
    - Kart tıklaması sadece ilgili kartın kendi gövdesinde dikeyde 55px değerine esnemesini, içindeki `lblDesc` etiketinin 430x45px boyutunda metni sarmalamasını (Wrap) ve üst GDI+ çizelgesinde ilgili alanın neon pembe parlamasını yönetir.
  - **Çakışma Yasağı:** Hover akışı ile kart tıklama akışı tek panel üzerinde birbirini asla ezmemeli, roller net olarak ayrı kalmalıdır.

## ⚙️ 6. VERİ OKUMA VE KARARLI PARÇALAMA STANDARTLARI (Parsing Engine)
- **Dosya Tipi:** Standart CSV. Ayırıcı karakter kesinlikle Virgül (`,`) olarak baz alınacaktır.
- **Veri Temizliği (Crucial):** Satır virgüle göre split edildikten sonra, elde edilen her string verinin başında ve sonunda bulunabilecek tüm çift tırnak (`"`) ve boşluk karakterleri kesin olarak temizlenecektir (`.Trim('"', ' ')`).
- **Zaman Hassasiyeti (TagDate):** Tarihler `yyyy-MM-dd HH:mm:ss` formatında tam saniye hassasiyetiyle `DateTime` nesnesine parse edilecektir. Tüm veri listesi kronolojik olarak (eskiden yeniye) saniye bazlı sıralanacaktır.
- **Nesne Tabanlı İstasyon/Robot Belirleme:** `TagName` string'inin içinde açıkça hangi alt grup kodu (Örn: `8F35`, `8Y50`, `9J39`, `8C70` vb.) geçiyorsa, log nesnesinin `StationName` property'sine doğrudan o değer atanacaktır. String içinde `-R1`, `.R2` gibi robot ekleri varsa bunlar temizlenip `RobotName` property'sine yazılacaktır. Arayüzde asla `"-"` görünmeyecektir.

## 🔒 7. KATI İŞ MANTIĞI VE %100 İSTASYON İZOLASYONU (Quarantine)
- **Sinyal Sonu Tanımları:**
  - `.Outputs.Transactionend` (Sadece `True` olanlar geçerli bir üretim penceresi başlatır/kapatır).
  - `.Outputs.ST_DownCondition` (True = Arıza başlangıcı, False = Arıza bitişi).
  - `.Errors.Alarm.DiscreteAlarm` (True = Kök neden adayı alarm sinyali).
- **Ortak Çevrim / Ortak Referans Mantığı:** İstasyonun altındaki tüm robot assetlerinin (R1, R2, R3 vb.) kendilerine özel ayrı bir Transactionend sinyali yoktur. İstasyon ve o istasyona bağlı tüm robotlar, üretim zaman aralıklarını (pencerelerini) belirlemek için istasyonun kendi meşru ortak `.Outputs.Transactionend` sinyalini ortak referans alır. Bir robotun arıza durumu (ST_DownCondition) işlenirken, o robotun bağlı olduğu üst istasyonun üretim çevrim pencereleri baz alınacaktır.
- **Nesne Tabanlı Katı İzolasyon (Zero Cross-Contamination):** Filtreleme ve mantık motoru çalışırken string araması (`.Contains`) KESİNLİKLE YAPILMAYACAKTIR. Filtreleme doğrudan nesne eşitliği üzerinden yürütülecektir: `if (log.StationName == selectedStation)`. Bir istasyonun ekranına veya raporuna, ismi açıkça o istasyonla birebir eşleşmeyen HİÇBİR log veya duruş satırı zamanı ne olursa olsun SIZAMAZ, eklenemez, tamamen bloke edilir.
- **Katı Üretim Çevrimi Sınırı:** Bir istasyonda arıza (`ST_DownCondition = True`) işlenebilmesi için, o arızanın zaman damgasının kesinlikle o istasyonun kendi meşru iki `Transactionend = True` sinyali arasında gerçekleşmiş olması şarttır. Dosyada ilgili istasyona ait hiç `Transactionend` verisi yoksa, o istasyonun Üretim Çevrimleri ve Arıza listesi **TAMAMEN BOMBOŞ** kalmalıdır (Asla sahte veya varsayılan çevrim uydurulmayacaktır).
- **Arızanın Çevrimle Kesilmesi (Downtime Splitting):** Devam eden bir arıza varken araya yeni bir `Transactionend = True` girerse, arıza o noktada bölünür. İlk parça o çevrimdeki ilk alarmı kök neden alır. İkinci parça yeni çevrim zamanında başlar ve gerçek `Down=False` ile biter, kök nedenini yeni çevrimde arar (bulamazsa Boş Alarm olarak turuncu listelenir).
- **Ham Veri Serbestliği:** `Ham Veri` sekmesi bu filtrelerden tamamen muaf olup, CSV'deki tüm satırları saniyeleriyle birlikte ham olarak her koşulda listeler.

## 📦 8. SAHADAN BAĞIMSIZ DAĞITIM VE YAYINLAMA STANDARTLARI (SELF-CONTAINED DEPLOYMENT)
- **Hedef Bilgisi Bağımsızlığı (.NET Runtime Isolation):** Projenin hedef bilgisayarlardaki .NET çalışma zamanı (Runtime) bağımlılığını ve versiyon uyumsuzluk risklerini sıfıra indirmek amacıyla, yayınlama mimarisi Self-Contained (Kendine Yeten) & Single-File (Tek Dosya) olarak kilitlenmiştir.
- **.csproj Konfigürasyon Standartları:** Proje dosyasında (DownTracker.csproj) her yayında runtime kütüphanelerinin .exe içine gömülmesini sağlayan şu parametreler kalıcı olarak korunacaktır:
  ```xml
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <PublishReadyToRun>true</PublishReadyToRun>
  ```
- **Üretim ve Canlıya Alma Komutu (Deployment Command):** Proje sahaya taşınırken her zaman terminal üzerinden şu optimize edilmiş paketleme komutuyla derlenecektir:
  ```powershell
  dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
  ```
- **Çıktı Güvencesi:** Bu komut sonucunda `bin/Release/net10.0-windows/win-x64/publish/` klasöründe oluşan tek bir `DownTracker.exe` dosyası, hedef bilgisayarda hiçbir .NET sürümü yüklü olmasa dahi tamamen bağımsız ve sıfır kurulum (Portable) mantığıyla çalışır.
