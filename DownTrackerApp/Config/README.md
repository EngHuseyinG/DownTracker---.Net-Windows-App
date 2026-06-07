# 🚀 DownTracker - Endüstriyel Duruş ve Alarm Analiz Portalı

[![C#](https://img.shields.io/badge/Language-C%23-purple.svg?style=for-the-badge&logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![.NET Core 10](https://img.shields.io/badge/Platform-.NET%20Core%2010.0-blueviolet.svg?style=for-the-badge&logo=.net)](https://dotnet.microsoft.com/)
[![Windows Forms](https://img.shields.io/badge/UI-Windows%20Forms%20(GDI%2B)-blue.svg?style=for-the-badge)](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)
[![Architecture](https://img.shields.io/badge/Architecture-MVVM-brightgreen.svg?style=for-the-badge)](#-mimari-tasarım-mvvm)

DownTracker, endüstriyel üretim hatlarındaki (örneğin robotik kaynak, montaj ve taşıma hatlarındaki) **Kepware ve PLC loglarını** saniye ve milisaniye hassasiyetiyle analiz eden, üretim çevrimlerini çıkaran ve arıza/duruş durumlarının kök nedenlerini (Root Cause) otonom olarak teşhis eden **kurumsal bir masaüstü diagnostic portalıdır.**

Spagetti kod yığınlarından tamamen arındırılarak **modern MVVM (Model-View-ViewModel) mimarisiyle** sıfırdan refaktör edilmiş ve Windows işletim sisteminin tema kısıtlamalarını aşan **%100 Saf C# ve GDI+ dinamik arayüz standartlarıyla** donatılmıştır.

---

## 📸 Temel Ekranlar ve Fonksiyonlar

Uygulama, endüstriyel veri analiz ihtiyaçlarını karşılayan 6 bağımsız modülden (ekrandan) oluşmaktadır:

### 1. 🔍 Down Conditions (Arıza & Duruş Analizi)
* **GDI+ Çizelge Zaman Tüneli:** Arızaları anlık olarak neon parlamalarla (`Color.FromArgb(255, 0, 85)`) görselleştiren, farenin hover durumuna göre Kepware tag verisini tepe diagnostic panelinde anlık olarak güncelleyen etkileşimli zaman çizelgesi.
* **Dinamik Kart Listesi:** Windows tema motorunun (`uxtheme.dll`) kısıtlamalarından arındırılmış, tıklanıldığında dikeyde genişleyen ve Kepware tag yollarını otomatik saran (`WrapMode`) özel GDI+ kart listesi.
* **Gelişmiş Filtreleme ve Sıralama:** Alarmlı/alarmsız arıza durumlarını kronolojik veya süreye göre milisaniye hassasiyetli sıralama.

### 2. 🔄 Çevrim Analizi (Cycle Analysis)
* İstasyonların ve bağlı robotların üretim zaman aralıklarını belirlemek için ortak `.Outputs.Transactionend` sinyalini referans alan analiz aracı.
* Çevrim sürelerini, duruş adetlerini ve toplam duruş sürelerini neon yeşili (`Color.FromArgb(0, 230, 118)`) veri satırlarıyla listeler.

### 3. 📋 Ham Veri İzleme (Raw Logs)
* CSV log dosyasındaki tüm satırları saniye ve tag bazlı kronolojik olarak listeler. İş mantığı filtrelerinden muaf olarak sahadaki tüm ham sinyal akışını inceleme imkanı sunar.

### 4. 🧙‍♂️ Grafana Sorgu Sihirbazı (Grafana Query Wizard)
* Kullanıcının herhangi bir sorgu butona basmasına gerek kalmaksızın, veri bağlaması (data binding) üzerinden tarih, kanal ve varlık (asset) girişlerine göre **otomatik olarak optimize SQL sorgusu üreten** asenkron view model yapısı.
* Tek tıkla clipboard'a kopyalama ve otonom durum göstergesi.

### 5. 📥 P360 Import Sihirbazı (P360 Import Wizard)
* Harici paket bağımlılığını (ClosedXML, EPPlus) ortadan kaldıran saf C# OpenXML Zip yapısıyla otonom Excel şablonu ve P360 import dosyası üreticisi.
* Canlı neon yeşili kesikli kenarlıklarla çevrelenmiş sürükle-bırak (Drag-Drop) dosya yükleme alanı.

### 6. ❌ P360 Hatalı Import Sihirbazı (P360 Faulty Import Wizard)
* P360 sistemine yüklenen eski Excel listesi ile Grafana hatalı alarm çıktı dosyasını (CSV) asenkron olarak parse edip otonom olarak eşleştiren ve kurtarma paketi üreten gelişmiş diagnostic aracı.
* Yatay kaydırma çubuğu ile buton alanının üst üste binmesini engelleyen, sequential (dikey akışlı) dikey kaydırma (AutoScroll) yerleşimi.

---

## 🏛️ Mimari Tasarım (MVVM)

DownTracker projesinde, UI (Görünüm) ile İş Mantığı (Logic) ve Veri Modeli birbirinden tamamen izole edilmiştir. Klasör yapısı ve katmanların rolleri şu şekildedir:

```
DownTrackerApp/
├── Models/              # İş mantığı ve veri modelleri (Stateless Parsing & Data Objects)
│   ├── LogEntry.cs      # Ham log satır şeması
│   ├── Cycle.cs         # Üretim çevrim şeması
│   ├── DownCondition.cs # Duruş/arıza şeması
│   └── ParsingEngine.cs # CSV/Excel okuma ve saha izolasyon algoritması
│
├── ViewModels/          # Durum (State) yönetimi ve arayüz dışı iş mantığı
│   ├── MainViewModel.cs          # Global veri setleri, yükleme durumları ve durum çubuğu mesajı
│   ├── DownConditionsViewModel.cs# Sıralama ve duruş filtreleme durumları
│   ├── CycleAnalysisViewModel.cs # Çevrim analizi durumları
│   ├── GrafanaWizardViewModel.cs # Otomatik SQL sorgu üretim motoru
│   ├── ImportWizardViewModel.cs  # P360 dönüşüm durumları
│   └── FaultyImportViewModel.cs  # Hatalı alarm eşleştirme ve kurtarma durumları
│
└── Views/               # Arayüz bileşenleri (Sadece UI çizimi, Grid stilleri, Event Binding)
    ├── MainForm.cs      # Navigasyon ağacı (tvStations) ve sekmeleri barındıran ana kabuk form
    ├── MainForm.Designer.cs
    ├── DownConditionsView.cs     # Down Conditions ekranı (UserControl)
    ├── CycleAnalysisView.cs      # Çevrim Analizi ekranı (UserControl)
    ├── RawLogsView.cs            # Ham Veri ekranı (UserControl)
    ├── GrafanaWizardView.cs      # Grafana ekranı (UserControl)
    ├── ImportWizardView.cs       # Import Sihirbazı ekranı (UserControl)
    ├── FaultyImportView.cs       # Hatalı Import Sihirbazı ekranı (UserControl)
    └── GuideForm.cs              # Kullanıcı Kılavuzu pop-up formu
```

### ⚡ Veri Bağlama ve Haberleşme Akışı
* ViewModeller `INotifyPropertyChanged` arayüzünü uygulayarak kendi özelliklerinde (properties) oluşan değişimleri arayüze otomatik olarak yansıtır.
* Arayüz bileşenlerindeki asenkron veya ağır işlemler ViewModel tarafında `Task.Run` ve `async/await` mimarisiyle arka planda (Background Thread) yürütülür, böylece UI Thread kesinlikle kilitlenmez.
* Alt UserControl'lerde oluşan durum çubuğu mesajları ve yükleme ekranı (overlay) tetikleyicileri, `MainViewModel` üzerinden ana formla güvenli ve eşzamanlı bir şekilde haberleşir.

---

## 🎨 Tasarım Standartları ve UI/UX Kuralları

* **Premium Koyu Tema:** Zemin rengi olarak `#0F0F0F` ve `#121212` tonları esas alınmıştır.
* **GDI+ Kenarlık Çizimleri:** Dosya sürükleme ve import panellerinde kesikli (`DashStyle.Dash`) kenarlıklar neon turkuaz (`#00E5FF` / `RGB 0, 229, 255`) fırçalarla runtime olarak çizilir.
* **Sıfır Boşluklu İnce Scrollbar:** DataGridView altındaki çirkin Windows scrollbar'ları gizlenerek, gridin hemen altına `DockStyle.Bottom` ile hizalanan 10px yüksekliğinde modern yatay kaydırma çubukları (`HScrollBar`) yerleştirilmiştir.
* **F1 Kısayolu:** Form seviyesinde `KeyPreview = true` yapılandırılarak kullanıcı uygulamanın herhangi bir ekranındayken `F1` tuşuna bastığında Kullanıcı Kılavuzunun otonom olarak açılması sağlanmıştır.

---

## 🛠️ Kurulum ve Derleme Standartları

Uygulamanın sahaya bağımsız taşınabilir (Self-Contained) olarak dağıtılması ve hedef makinelerde .NET Runtime bağımlılığı olmaksızın tak-çalıştır açılabilmesi için `.csproj` yapılandırmasında şu taşınabilirlik ayarları mühürlenmiştir:

```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishReadyToRun>true</PublishReadyToRun>
```

### 💻 Bağımsız Canlıya Alma Komutu
Projeyi tek bir bağımsız `.exe` dosyası halinde paketlemek için proje kök dizininde şu komut çalıştırılır:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
Derleme sonucunda oluşan bağımsız executable dosya `bin/Release/net10.0-windows/win-x64/publish/DownTracker.exe` konumunda üretilir.

---

## 🐛 Çözülen Kritik Saha Bugları ve Teşhis Kataloğu

* **Zeytin Yeşili Problemi (uxtheme.dll):** DataGridView aktif odaklandığında Windows tema motorunun zorla dayattığı rengin engellenmesi için detay listelerinde DataGridView kullanımı yasaklanıp `%100 Saf GDI+ Dinamik Kart` mimarisine geçilmiştir.
* **Controls.Clear() Yasağı:** Tasarım anında eklenen sarmalayıcı nesnelerin (tabMain, splitMain) bellekten silinerek uygulamanın çökmesini önlemek için kontroller hiyerarşik eklenmiş, geçişler `BringToFront()` / `SendToBack()` ile yapılmıştır.
* **TreeView Daraltma (CollapseAll) Kilidi:** Ağaç yapısı daraltıldıktan sonra WinForms durum makinesinin kilitlenmesini önlemek için seçim önce `null` yapılıp, ardından kök düğüme odaklanmaktadır.
* **Propagation Delay Çözümü:** Robotun duruş sinyalini ana istasyondan önce tetiklemesinden kaynaklanan "Boş Arıza" (Boş Alarm) bug'ı, duruşların kök nedenlerinin saniye bağımsız olarak iki `TransactionEnd = True` çevrim sınırları içerisinde taranmasıyla çözülmüştür.

---

### 👑 Designed by Hüseyin Gürel
*Bu proje, Ford Otosan üretim sahası veri otomasyon ihtiyaçlarına yönelik optimize edilerek geliştirilmiştir.*
