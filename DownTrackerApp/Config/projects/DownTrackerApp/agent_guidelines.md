# 📘 DownTracker App - Geliştirici ve Agent Kılavuzu (Proje İndeksi)

Bu kılavuz, DownTracker App projesinin modüler kılavuz haritasıdır. İlgili modül üzerinde geliştirme yaparken doğrudan o modülün kılavuz dosyasını sistem rolü (System Prompt) olarak ithal edip baz alınız.

---

## 🏛️ Oturum Başı Yükleme Sırası

```
Katman 0 (Evrensel):  ../../universal/00_meta_agent_blueprint.md
                      ../../universal/01_core_methodology.md
Katman 1 (.NET):      ../../platforms/dotnet/01_dotnet_standards.md
Katman 2 (Proje):     Bu dosya + göreve özel modül
```

---

## 🗂️ Kılavuz Modülleri

### Katman 0 — Evrensel
0. 📐 **[00_meta_agent_blueprint.md](../../universal/00_meta_agent_blueprint.md)** (Ajan Kurulum Şablonu)
   * Tüm yazılım projelerinde kılavuz dosyalarının mimarisini ve token tasarrufu standartlarını belirleyen evrensel meta şablon.

1. 🔬 **[01_core_methodology.md](../../universal/01_core_methodology.md)** (Çekirdek Metodoloji ve Token Tasarrufu)
   * Token tasarrufu kuralları, nokta atışı parça kod yazma zorunluluğu, laf kalabalığı yasağı ve otomatik kılavuz güncelleme anayasası.

### Katman 1 — .NET Teknoloji
T. 🟣 **[01_dotnet_standards.md](../../platforms/dotnet/01_dotnet_standards.md)** (.NET Platform Standartları)
   * DataGridView/uxtheme.dll yasağı, Controls.Clear() yasağı, GDI+ kart mimarisi, Task.Run async şablonu, Self-Contained deploy ve WinForms platform kara listesi.

### Katman 2 — Proje
2. 📂 **[02_industrial_data_rules.md](./02_industrial_data_rules.md)** (Endüstriyel Saha ve Algoritma Anayasası)
   * İstasyon bazlı katı nesne izolasyonu, `.Outputs.TransactionEnd` ortak referans mantığı, duruş kesme/bölme algoritmaları, iki TransactionEnd arası çevrim bazlı alarm tarama mantığı.

3. 📂 **[03_ui_ux_standards.md](./03_ui_ux_standards.md)** (Görsel Tasarım ve Arayüz Kuralları)
   * Premium Koyu Tema (#0F0F0F) kuralları, WinForms odak kilidi yasağı, FlowLayoutPanel tabanlı dinamik kartlar, hybrid UX (hover/click) ayrımı, UserControl tabanlı modüler ekran mimarisi.

4. 📂 **[04_performance_async.md](./04_performance_async.md)** (Asenkron Performans ve Performans Algısı)
   * UI Thread kilitlenme yasağı, Task.Run ve Async/Await kullanımı, yükleme overlay katmanı, MVVM (Model-View-ViewModel) mimari tasarımı ve data binding standartları.

5. 📂 **[05_deployment_release.md](./05_deployment_release.md)** (Sahadan Bağımsız Taşınabilir Dağıtım)
   * Self-Contained ve Single-File paketleme ayarları, `.csproj` yapılandırması ve terminal publish komutları.

6. 📂 **[06_troubleshooting_blacklist.md](./06_troubleshooting_blacklist.md)** (Saha Hata Kataloğu, Kara Liste ve Çözümler)
   * Grid odaklanma/uxtheme.dll bug'ı, Controls.Clear() yasağı, UI Thread kilitlenme yasağı, propagation delay çözümü, Hybrid UX çakışma çözümleri ve tvStations CollapseAll durum kilitlenmesi çözümü.
