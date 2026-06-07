# AGENT BEHAVIOR & SYSTEM PROMPT
WinForms arayüzünde DataGridView kullanılması kesinlikle yasaktır. Kart listelerini ve çizelge hover mantığını işletirken, tıklama ve hover rollerini asla birbirine karıştırmayacak, merkezi paneli sadece hover verisiyle besleyeceksin.

---

# 3. Görsel Tasarım ve Arayüz Kuralları (UI/UX)

## 🎨 Renk Anayasası ve Temalandırma
- **Premium Koyu Tema:** Arayüz renk paleti ve temalandırması için ana zemin rengi olarak `#0F0F0F` (Premium Koyu Tema) esas alınacaktır.

## 🛑 DataGridView ve Windows Odak (Focus) Yasağı
- **Kök Sorun (Zeytin Yeşili Problemi):** Pop-up pencerelerde `DataGridView` kullanıldığında, Windows tema motoru (`uxtheme.dll`) aktif odaklanan kontrol satırlarına kendi varsayılan Focused/Selected State rengini (donuk zeytin yeşili/kirli sarı) zorla dayatır. Bu durum C# düzeyindeki `CellFormatting` veya `RowPrePaint` özellik atamalarını tamamen bypass ederek arayüzün kilitlenmesine neden olur.
- **Nihai Çözüm (%100 Saf C# Dinamik Kart Mimarisi):** Detay ekranlarında `DataGridView` kullanımı tamamen yasaklanmıştır. Bunun yerine, işletim sistemi tema müdahalelerinden %100 izole, runtime kod tabanlı (Pure Code-Behind) `FlowLayoutPanel` ve Dinamik Kart (`Custom Panel Cards`) mimarisi getirilmiştir. Kurallar tamamen GDI+ fırçalarının kontrolündedir.

## 📐 Milimetrik Sütun Hizalama Standartları
Tablosuz kart listesinde hizalamanın kaymaması için formun tepesinde statik bir başlık satırı (`pnlHeaderRow`) bulunur. Başlık ve alt kartların içindeki etiket genişlikleri dikey eksende milimetrik olarak şu ölçülerde sabitlenmiştir:
- **Başlangıç Zamanı:** 140px
- **Bitiş Zamanı:** 140px
- **Süre (ms/s):** 70px
- **Alarm / Durum Açıklaması:** 430px
- **Sol Padding Payı:** 25px (FlowLayoutPanel scrollbar çıktığında taşmayı ve kırpılmayı önlemek amacıyla bırakılmıştır).
- **Dinamik Başlık Sıralama:** Başlık sütun etiketlerine tıklandığında veriler ilgili alana göre katı tipli (Type-Safe) sıralanır ve sıralama yönüne göre (▲ / ▼) simgeleri dinamik olarak güncellenir. İlk yüklemede varsayılan olarak "Başlangıç Zamanı" kolonuna göre kronolojik (eskiden yeniye) dizilim uygulanır.

## 🔀 Hover & Click Etkileşim Rollerinin Kesin Ayrımı (Hybrid UX)
- **Merkezi Sinyal Detay Paneli (`pnlSignalSummary` & `lblCentralSignalInfo`):**
  - Bu panel YALNIZCA VE SADECE zaman çizelgesi üzerindeki `MouseMove` (Hover) hareketlerine endekslidir.
  - Fare dikey alarm çizgilerinin (3px kalınlığında olanlar) veya kırmızı duruş şeritlerinin üzerine geldiğinde anlık Kepware verisini bu panelde gösterir.
  - Fare boşluğa çıktığı an panel anında temizlenerek varsayılan yönlendirme metnine (`"İncelemek istediğiniz arıza veya alarm çizgisinin üzerine gelin..."`) geri döner.
- **Alt Kart Tıklama İzolasyonu:**
  - Alttaki dinamik kart listesinde yapılan tıklama (`Click`) olayları, tepe panelindeki metni kesinlikle değiştiremez veya etkileyemez.
  - Kart tıklaması sadece ilgili kartın kendi gövdesinde dikeyde 55px değerine esnemesini, içindeki `lblDesc` etiketinin 430x45px boyutunda metni sarmalamasını (`Wrap`) ve üst GDI+ çizelgesinde ilgili alanın neon pembe parlamasını yönetir.
- **Çakışma Yasağı:** Hover akışı ile kart tıklama akışı tek panel üzerinde birbirini asla ezmemeli, roller net olarak ayrı kalmalıdır.

## 📊 Alt Bar KPI Gösterge Standartları
- **Çerçevesiz Alt Panel Entegrasyonu:** En alttaki istatistik barı (`pnlFooter`), dikey yüksekliği `Height = 45` olan ve yatay/dikey kaydırma çubukları kapatılmış (`AutoScroll = false`, `Padding = new Padding(15, 12, 15, 5)`) statik bir `FlowLayoutPanel` olarak yapılandırılmıştır. Herhangi bir çerçeve veya kenarlık barındırmaz.
- **Mantıksal Ayrım ve Hizalama:** Fabrika genelindeki toplam istatistikler (`Genel - Toplam`) ile seçili durum verileri (`Seçili - Toplam`) etiketler düzeyinde ayrılmış ve aralarındaki boşluk `Padding`/`Margin` değerleri ile dengelenmiştir.
- **Renk ve Durum Senkronizasyonu:** İstatistik kartlarının yazı renkleri, sol alt durum filtrelerinin neon/yumuşak renkleriyle birebir eşlenmiştir:
  - **Alarmlı Duruşlar:** Neon Kırmızı (`Color.FromArgb(255, 0, 85)`)
  - **Alarmsız Duruşlar:** Yumuşak Turuncu (`Color.FromArgb(255, 145, 0)`)
  - **Toplam Duruşlar:** Soluk Beyaz / Gri (`Color.FromArgb(224, 224, 224)`)
- **İmza Alanı & Durum Çubuğu Yüksekliği:** En alttaki `statusStrip` durum çubuğunun yüksekliği dikey nefes alma alanı sağlamak için `Height = 35` (AutoSize = false) olarak sabitlenmiştir. Durum çubuğu içerisine, Spring destekli (`lblSpringSpacer`) ve sağa hizalanmış (`TextAlign = ContentAlignment.MiddleRight`) İtalik `"Designed by Hüseyin Gürel"` imza kuralı uygulanmalı, sol mesaj alanı (`lblStatus`) ise `TextAlign = ContentAlignment.MiddleLeft` olarak dikey ortalanmalıdır.

## 🏗️ Zaman Çizelgesi GDI+ Çizim Kuralları
- **Çizelge Taban Çubuğu (Zemin):** TransactionEnd sinyalinin temsil ettiği tüm üretim çevrimi antrasit gri (`#2D2D2D`) şerit olarak çizilir. Çevrim sınırlarını temsil eden `"TransactionEnd (Başlangıç)"` ve `"TransactionEnd (Bitiş)"` etiketleri (Bold 8pt Segoe UI, Beyaz) dikey yığılmayı ve operatör göz yorgunluğunu azaltmak amacıyla çizelgenin dip ekseninde kendi zaman damgalarının tam üst hizasında alt alta çizilir.
- **Default (Seçilmeyen) Kartlar ve Çubuklar:** Alarm veya duruş segmenti ayrımı yapılmaksızın, seçilmeyen tüm alt kartlar ve üst çizelgedeki arıza alanları milisaniyelik tam oranlamayla (mapping) Net Duruş Kırmızısı (`Color.FromArgb(255, 74, 74)`) olarak boyanır. Metinler koyu antrasit (`#141414`) yapılarak kontrast korunur.
- **Seçilen Kart ve Parlama (Cross-Highlighting):** Kullanıcı listeden bir karta tıkladığında, `_selectedSegmentIndex` kontrolü en üst öncelikli elenir. Seçilen kart ve üst çizelgedeki izdüşümü aynı milisaniyede Canlı Neon Pembe (`Color.FromArgb(255, 0, 85)`) rengine bürünerek kusursuz bir çift yönlü diagnostic reaksiyon verir.

## 🧭 Panel ve Hiyerarşi Düzeni
- **UserControl Tabanlı Ekran Mimarisi:** `tabMain` içerisindeki tüm ekran sekmeleri (`Down Conditions`, `Çevrim Analizi`, `Ham Veri İzleme`, `Grafana Sorgu Sihirbazı`, `P360 Import Sihirbazı`, `P360 Hatalı Import Sihirbazı`), `MainForm`'un kod karmaşıklığını önlemek ve modülerliği sağlamak amacıyla `Views` klasörü altında ayrı birer `UserControl` (`DownConditionsView`, `CycleAnalysisView`, `RawLogsView`, `GrafanaWizardView`, `ImportWizardView`, `FaultyImportView`) olarak yapılandırılmıştır.
- **Z-Order ve Dock Hizalaması:** Form üzerindeki docked kontrollerin üst üste binerek (overlapping) birbirini örtmesini ve alan çalmasını engellemek için, `Dock = DockStyle.Fill` olan `splitMain` kontrolü her zaman Z-Order'ın en üstünde tutulmalıdır (`splitMain.BringToFront()`). `pnlTop` ve `statusStrip` gibi dış panel sınırları `SendToBack()` edilmeli, `pnlFooter` ise `statusStrip`'in hemen üzerinde yer alacak şekilde `BringToFront()` edilmelidir.
- **Minimalist Sol Panel ve Esnek Sağ Panel:** Sol taraftaki istasyon/robot ağacının genişliği (`splitMain.SplitterDistance`) endüstriyel standartlar gereği `180px` (min: `150px`) olarak sabitlenmiştir. Sağ taraftaki `tabMain` ise `Dock = DockStyle.Fill` olarak tüm alanı kaplamakta, üst panellerle çakışmaması için `splitMain.Panel2.Padding` değeri `(0, 0, 0, 0)` olarak sıfırlanmış ve hiyerarşi `pnlBreadcrumb` ile dikeyde hizalanmıştır.
- **Üretim Çevrimleri Grid Standardı (`dgvProductionCycles`):** Üretim Çevrimleri listesinde de tıpkı Down Conditions listesinde olduğu gibi en sol tarafta (0. indeks) meşru 'Varlık Adı' (`DataPropertyName: StationName` veya `AssetName`) sütunu yer alır. Bu sayede iki ana tablonun görsel hiyerarşisi ve veri takibi sahadaki çapraz kontaminasyonu önleyecek şekilde eşitlenmiştir.
- **Controls.Clear() Yasağı (Disposed Koruması):** Sağ panel kontrol listesini yenilemek amacıyla asla `Controls.Clear()` çağrısı yapılamaz! Bu çağrı, tasarım anında (Design-time) oluşturulan `tabMain` nesnesini hafızadan tamamen silerek (disposed) uygulamanın çökmesine ve sekmelerin kaybolmasına neden olur. Kontroller hiyerarşik eklenmeli ve `BringToFront()` / `SendToBack()` ile yönetilmelidir.
- **TreeView Odaklanma ve CollapseAll Kilidi:** Ağaç yapısı daraltıldıktan (`CollapseAll`) sonra WinForms'un durum makinesinin kilitlenmesini ve 'Tüm Varlıklar' filtresinin tetiklenmemesini önlemek için seçim önce `null` yapılmalı, ardından manuel olarak kök düğüme eşitlenmeli ve odaklanılmalıdır:
  ```csharp
  tvStations.SelectedNode = null;
  tvStations.SelectedNode = tvStations.Nodes[0];
  tvStations.Focus();
  ```
- **İlk Yükleme Düzeni:** CSV/Excel dosyası sisteme ilk yüklendiğinde, ağaç yapısı karmaşayı önlemek için `CollapseAll()` edilmeli ve sadece en üstteki kök düğüm (`Nodes[0].Expand()`) genişletilmelidir. Dinamik arama esnasındaki `ExpandAll()` mekanizması ise korunmalıdır.

## 🎛️ Durum Filtreleri Standartları
- **Kurumsal İsimlendirme:** Sol alt köşede yer alan `pnlFilters` içerisindeki filtre butonlarının metinlerinde parantez içi renk veya süsleme belirten ifadeler yer almaz. Arayüz sade ve kurumsal standartlarda tutulur:
  - `"Tüm Arızalar / Duruşlar"` (Herhangi bir filtreleme yapmadan tüm verileri listeler)
  - `"Alarmlı Arızalar"` (Kök nedeni olan ve alarm eşleşen duruşları listeler)
  - `"Alarmsız Arızalar"` (Herhangi bir alarm eşleşmesi olmayan boş duruşları listeler)

## ❓ Yardım Butonu ve Menü İkon Senkronizasyonu
- **Yardım Butonu ve F1 Kısayol Aktivasyonu:** Sağ üst köşedeki yardım butonu çerçevesiz (`BorderSize = 0`), `BackColor = #151515` (Color.FromArgb(21, 21, 21)), `ForeColor = #00E5FF` (Color.FromArgb(0, 229, 255)), `FlatStyle = FlatStyle.Flat` ve sınır rengi turkuaz olarak yapılandırılmıştır. Metni `"❓ UYGULAMA KULLANIM KILAVUZU"`dur. Hover durumunda arka plan parlak turkuaz (`Color.FromArgb(0, 255, 255)`), metin siyah (`Color.Black`) olmalıdır. Ayrıca, kullanıcı formun herhangi bir yerinde `F1` tuşuna bastığında kullanım kılavuzunun (`GuideForm`) doğrudan ve otonom olarak açılması için form seviyesinde `KeyPreview = true` ve `F1` klavye dinleyicisi aktif edilmiştir.
- **Sekme ve Rehber Başlık Yapılandırması:**
  - **Ana Ekran Sekmeleri (Vektörel ImageList):** Windows GDI+ / uxtheme boş kutu (□) render hatalarını önlemek amacıyla tüm font tabanlı ikonlar kaldırılmış, yerine runtime GDI+ vektörel çizim motoru (`CreateImageList(16)`) entegre edilmiştir. Sekme metinleri sade ve kurumsal isimlerden oluşur:
    1. `"Down Conditions"` (İndex 0: Dikey Bar Grafiği)
    2. `"Çevrim Analizi"` (İndex 1: Dairesel Çevrim Oku)
    3. `"Ham Veri İzleme"` (İndex 2: Tablo Izgarası)
    4. `"Grafana Sorgu Sihirbazı"` (İndex 3: Terminal script prompt `>_` - Güncellendi!)
    5. `"P360 Import Sihirbazı"` (İndex 4: Sayfa ve aşağı ok)
  - **Rehber Ekranı Sol Menüsü (Vektörel ImageList):** Sol kılavuz menüsü `Width = 280` ve form boyutu `Width = 1020` olarak sabitlenmiştir. Butonlar 24x24 piksel boyutunda pürüzsüz vektörel ikonlar içeren `_guideImageList` kullanır. Butonlarda `Padding = new Padding(12, 0, 0, 0)` kullanılmış ve buton metinlerinin önüne `"  "` çift boşluğu eklenerek ikon ve metin arası boşluk enjekte edilmiştir. Hizalamalar ise `ImageBeforeText` ve sola yaslı (`MiddleLeft`) olarak kilitlenmiştir:
    * `"Nasıl Çalışır / Proses Açıklaması"` (İndex 5: Bilgi dairesi "i")
    * `"Down Conditions Ekranı"` (İndex 0)
    * `"Çevrim Analizi Ekranı"` (İndex 1)
    * `"Ham Veri Ekranı"` (İndex 2)
    * `"Grafana Sorgu Sihirbazı Ekranı"` (İndex 3)
    * `"P360 Import Sihirbazı Ekranı (5. Ekran)"` (İndex 4)
    * `"P360 Hatalı Import Sihirbazı (6. Ekran)"` (İndex 6: X işareti, Neon Kırmızı `#FF4A4A`)
    * `"Uygulama Hakkında / Künye"` (İndex 5: Bilgi dairesi "i")

## 📥 5. Ekran (P360 Import Sihirbazı) UI/UX Standartları

- **Boşluksuz İnce Scrollbar Tasarımı (Zero-Gap Grid Scrollbar):**
  - Girdi (`dgvInputPreview`) ve çıktı (`dgvOutputPreview`) önizleme gridlerinin altlarında C# WinForms varsayılan antrasit/gri ızgara boşluğunun görünmesini engellemek için, gridler şeffaf sarmalayıcı panellere (`pnlInputGridWrapper` ve `pnlOutputGridWrapper`, `Dock = DockStyle.Fill`) yerleştirilmiştir.
  - Gridlerin kendileri `Dock = DockStyle.Top` yapılarak yükseklikleri tam içerik boyutlarına sabitlenmiştir (girdi için `62px`, çıktı için `62px` - Calibri 12pt Bold başlık + Calibri 11pt veri satırı + kenarlıklar dahil).
  - 10px yüksekliğindeki ultra ince yatay kaydırma çubukları (`hScroll`), sarmalayıcı paneller içinde `Dock = DockStyle.Bottom` yapılarak gridlerin hemen altına sıfır boşlukla hizalanmıştır.
- **Canlı Neon Yeşil Kenarlıklar (GDI+ Custom Paint Border):**
  - **pnlImportDragDrop (Sürükle-Bırak / Excel Yükleme Butonu):** Varsayılan gri Windows kenarlığı kaldırılmıştır (`BorderStyle = BorderStyle.None`). Panelin `Paint` olayında GDI+ ile özel 1px genişliğinde canlı turkuaz (`#00E5FF` / `RGB 0, 229, 255`) kesikli (`DashStyle.Dash`) bir sınır çizgisi çizdirilir. Docked etiketinin bu çizgiyi örtmesini engellemek için panele `Padding = new Padding(1)` verilmiştir. Form boyutlandırma durumlarında kenarlıkların bozulmaması için `Resize` olayı `Invalidate()` tetikleyicisine bağlanmıştır. Buton yüksekliği 6. Ekrandaki yükleme butonları ile eşitlenerek `Height = 280` olarak ayarlanmıştır.
  - **pnlTop (Ana Ekran Duruş/Alarm Sürükleme Alanı):** Yukarıdaki standartla tam simetri sağlamak için `pnlTop` kenarlığı da `BorderStyle.None` yapılmış ve `Paint` ile `Resize` olayları üzerinden 1px canlı turkuaz (`#00E5FF` / `RGB 0, 229, 255`) kesikli (`DashStyle.Dash`) kenarlıkla sarılmıştır. Ana bilgilendirme yazısının rengi de bu turkuaz renkle eşitlenmiştir.
- **Kesintisiz Turkuaz Tipografi ve Normalize İkonlar:**
  - Dosya sürükleme/seçme alanındaki kılavuz metinler (`lblImportDragDrop` ve `lblDragDrop`), arayüzün ilk açılışından, dosya sürükleme anına ve işlemin sıfırlanıp başa döndüğü tüm aşamalarda her zaman canlı turkuaz (`RGB 0, 229, 255`) renkte tutulur. `lblImportDragDrop` etiketi `Segoe UI 11F Bold` stiline sahiptir (ana ekrandaki `lblDragDrop` ile font büyüklükleri eşitlenmiştir), başında `"📩 1. INPUT: "` ve sonunda `" (.xlsx)"` ibareleri yer alır ve `"IIoT"` hariç tüm metin büyük harfle yazılır.
  - İlk örnek excelin başlığında (`lblInputHeader`) `"📩 1.INPUT: BEKLENEN INPUT EXCEL FORMATI VE ÖRNEK VERİ HATTI"` biçiminde `"1.INPUT: "` ibaresi yer alır.

## ❌ 6. Ekran (P360 Hatalı Import Sihirbazı) UI/UX Standartları

- **Çift Sürükle-Bırak Panel (Old Excel & Grafana CSV / Excel Yükleme Butonları):**
  - İki adet yan yana kesikli çizgili flat sürükleme paneli yerleştirilir. Sınır çizgileri GDI+ Paint olayında özel kesikli olarak çizdirilir. Yükseklikleri 5. Ekrandaki butonla uyumlu olacak şekilde (buton net yüksekliği `265px` korunarak) `pnlDragContainer` yüksekliği `300px` (ve `Padding = new Padding(0, 20, 0, 15)`) olarak ayarlanmıştır.
  - Yatay kaydırma çubuklarıyla yükleme panellerinin çakışmasını/üst üste binmesini tamamen engellemek amacıyla `pnlDragContainer` kontrolü `Dock = DockStyle.Top` yapılarak 3. Output gridinin hemen altına dikey hiyerarşide eklenmiştir. Bu sayede tüm arayüz tek parça halinde kaydırılır (AutoScroll).
  - Eski Excel panelinin sınır çizgisi Turkuaz (`#00E5FF`), Grafana CSV panelinin sınır çizgisi Neon Kırmızı (`#FF4A4A`) olarak boyanır. Arka planları ise koyu tema uyumlu `#121212` (`RGB 18, 18, 18`) seviyesindedir.
  - Butonların içindeki metinler sırasıyla `"📩 1.INPUT: P360 SİSTEMİNE YÜKLENEN ESKİ EXCELİ BURAYA SÜRÜKLEYİN (.xlsx)"` ve `"📩 2.INPUT: GRAFANA HATALI ALARMLAR ÇIKTISINI BURAYA SÜRÜKLEYİN (.csv)"` olarak ve en sollarında `📩` ikonuyla `Segoe UI 11F Bold` font stilinde (ana dosya yükleme alanı ile eşitlenerek) normalize edilmiştir.
- **Tipografi ve Önizleme Gridleri (3'lü Dikey Önizleme Grid Mimarisi):**
  - **Grid 1 (dgvFaultyInputPreview):** `"📩 1. INPUT: P360 SİSTEMİNE IMPORT EDİLEN ESKİ ALARM LİSTESİ ÖRNEĞİ"` başlığı Canlı Turkuaz (`#00E5FF`) renktedir. Meşru 6 sütunlu formatta olup `FULLTAGNAME` = 800px, `DEVICENAME` = 110px, `COMMENT` = 280px, `ParentCategory` = 280px, `Category` = 120px, `Code` = 75px genişliğindedir. Sarmalayıcı panel dikey yüksekliği `170px` (grid `135px` + başlık `25px` + scrollbar `10px`) olarak ayarlanmıştır.
  - **Grid 2 (dgvGrafanaFaultyPreview):** `"📩 2. INPUT: GRAFANA HATALI IMPORT EDİLEN ALARM LİSTESİ"` başlığı Neon Kırmızı (`#FF4A4A`) renktedir. Tek sütun `SensorName` genişliği net 800 pikseldir. Sarmalayıcı panel dikey yüksekliği `135px` (grid `75px` + başlık `25px` + padding `15px` + scrollbar `10px` + tampon payı) olarak ayarlanarak ikinci veri satırının kırpılması engellenmiştir.
  - **Grid 3 (dgvFaultyOutputPreview):** `"📩 3. OUTPUT: P360'A TEKRAR IMPORT EDİLECEK  ALARM LİSTESİ"` başlığı Parlak Turuncu (`#FF9100`) renktedir. Meşru 6 sütunlu formatta olup `FULLTAGNAME` = 800px genişliğindedir. Sarmalayıcı panel dikey yüksekliği `115px` (grid `75px` + başlık `25px` + scrollbar `10px`) olarak ayarlanmıştır.
  - **Kaydırma Çubuğu Standardı:** Tüm bu 3 gridin de altında modern 10 piksellik ince yatay kaydırma çubuğu (`Height = 10`) ve `ScrollBars = ScrollBars.None` standartları kullanılarak verilerin üzerine kaydırma çubuğu binmesi engellenmiştir. `BindCustomScrollbar` metodu, sarmalayıcı panellerin yüksekliğini zorla sıfırlamak yerine panellerin kendi tanımlı asil yüksekliklerini korur ve kaydırma çubuğunu panelin en altına (`DockStyle.Bottom`) mühürler.
- **Otonom Eşleştirme Akışı:**
  - Eski Excel dosyası sürüklenip yüklendiğinde, asenkron olarak parse edilip sınıf hafızasına doldurulur ve ilk 10 satır `dgvFaultyInputPreview` gridine doldurulur.





