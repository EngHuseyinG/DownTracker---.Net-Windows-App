# AGENT BEHAVIOR & SYSTEM PROMPT
Dosya okuma ve parse işlemlerini asla UI Thread üzerinde yapamazsın. Arka plan görevlerini try-catch-finally yapısıyla sarmalayıp, overlay katmanını finally bloğunda kapatmak zorundasın.

---

# 4. Asenkron Performans ve Performans Algısı

## ⚡ UI Thread Kilitleme Yasağı ve Asenkron Çalışma
- **Temel Kural:** Ağır dosya yükleme, okuma, parsing ve veri işleme operasyonlarında UI Thread (arayüz iş parçacığı) kesinlikle kilitlenemez.
- **Asenkron Yapı:** Ağır işlemler `Task.Run` ve `async`/`await` kullanılarak arka planda (Background Thread) yürütülmelidir.

## 🌀 Yükleme Katmanı (Loading Overlay) ve İlerleme Motoru
- **Görsel Bildirim:** Dosya yükleme ve uzun süren işlemlerde, arayüzün kilitlendiği algısını yıkmak için Flat Antrasit renginde bir yükleme katmanı (`pnlLoadingOverlay`) devreye sokulmalıdır.
- **İlerleme Göstergesi:** Katman üzerinde animasyonlu bir Marquee ProgressBar ilerleme motoru çalıştırılmalıdır.
- **Güvenli Kapatma (Finally Bloğu):** Arka plan görevleri kesinlikle `try-catch-finally` bloğu ile sarmalanmalı ve yükleme katmanı `finally` bloğu içerisinde `Visible = false` yapılarak güvenli bir şekilde kapatılmalıdır. Bu mühürleme anayasası asla ihlal edilemez.

## 📊 Otomatik Grafana SQL Sorgu Üretim Motoru (MVVM)
- **Arka Planda Otomatik Tetiklenme:** Grafana SQL sorguları, kullanıcının bu tona tıklamasına gerek kalmaksızın, veri bağlaması (data binding) aracılığıyla view model üzerinde otomatik olarak tetiklenerek üretilir.
- **MVVM Sorgu Üretim Mantığı (`GrafanaWizardViewModel.cs`):**
  - Hafızada yüklü bir veri veya sol ağaçta seçili bir asset (varlık) olmasa dahi çalışabilecek şekilde saf string manipülasyonu ile view model içindeki `SqlQuery` hesaplama özelliği (property) olarak kurgulanmıştır.
  - View model üzerindeki giriş özelliklerinin (`Date`, `Channel`, `Asset`, `AllAssets`) anlık değişimlerinde (`OnPropertyChanged()`) `SqlQuery` değeri otomatik olarak yeniden hesaplanır ve veri bağlama üzerinden `GrafanaWizardView` arayüzündeki ilgili metin alanına yansıtılır.
  - Manuel sorgu oluşturma butonları tamamen kaldırılmıştır ve bu otomatik akış kesinlikle bozulmamalıdır.

## 🏛️ MVVM (Model-View-ViewModel) Mimari Yapısı ve Data Binding

Uygulamanın spagetti kod tabanından tamamen arındırılması ve ekran bazlı mantıksal birimlerin birbirinden izole edilmesi amacıyla **MVVM (Model-View-ViewModel)** tasarım deseni uygulanmıştır:

- **Model (M - DownTracker.Models):** Veri yapılarını (`LogEntry`, `Cycle`, `DownCondition`) ve dosyalardan okuma/parse etme işlemlerini yürüten saf algoritmaları (`ParsingEngine`) barındırır. UI veya sunum katmanından tamamen bağımsızdır.
- **ViewModel (VM - DownTracker.ViewModels):** Arayüzün durumunu (State) ve iş mantığını (Business Logic) saklar. `INotifyPropertyChanged` arayüzü ile arayüzdeki değişiklikleri dinler ve tetikler.
  - `MainViewModel`: Sol ağaçta seçili istasyon, asenkron yüklenen tüm log verileri, genel yükleme durumu (loading overlay) ve durum çubuğu mesajı gibi global durumları barındırır ve yönetir.
  - Ekran ViewModelleri (`DownConditionsViewModel`, `CycleAnalysisViewModel`, `RawLogsViewModel`, `GrafanaWizardViewModel`, `ImportWizardViewModel`, `FaultyImportViewModel`): Ekranların kendi yerel verilerini ve durumlarını barındırır, asenkron veriler yüklendiğinde veya filtreler değiştiğinde veriyi işleyip View katmanına sunar.
- **View (V - DownTracker.Views):** Arayüz elemanlarını ve görsel bileşenleri barındırır. Her ana ekran, `UserControl` sınıfından türetilen bağımsız bir bileşen olarak tasarlanmıştır.
  - `MainForm`: Sadece sol ağaç listesini (`tvStations`), genel yükleme overlay'ini ve sekmeleri (`tabMain`) barındıran taşıyıcı bir kabuktur.
  - Ekran View'leri (`DownConditionsView`, `CycleAnalysisView` vb.): Tasarım anında veya runtime'da ilgili ViewModel ile eşleştirilir. GDI+ çizimleri, grid hücre boyama ve custom scrollbar bağlama gibi tamamen arayüze özel (görsel) operasyonları üstlenir.
- **Veri Bağlama ve Haberleşme Akışı:**
  - View'ler, kendi ViewModel'lerindeki property'leri veri bağlama (Data Binding) veya event'ler aracılığıyla dinler.
  - ViewModel üzerinde bir veri değiştiğinde (`OnPropertyChanged`), View bunu otomatik olarak algılar ve kendi bileşenlerini (örneğin Grafana Wizard üzerindeki `SqlQuery` metin alanını) günceller.
  - Alt View'lerde gerçekleşen durum değişiklikleri (örneğin asenkron işlemin başlaması/bitmesi veya durum çubuğu mesajları), event'ler veya callback'ler aracılığıyla `MainViewModel`'e iletilir ve arayüz kilitlenmeksizin durum çubuğuna (`statusStrip`) yansıtılır.
  - View'ler, arka planda çalışan uzun işleri her zaman asenkron olarak ViewModel'ler üzerinden tetikler. ViewModel işi `Task.Run` ile arka plana atar, tamamlandığında veri setini günceller ve arayüze bildirim gönderir.
