# 📘 Evrensel Agent Kılavuzu — 3 Katmanlı Mimari Giriş Kapısı

Bu dosya, teknoloji ve projeden bağımsız olarak **her oturumun başında** yüklenecek evrensel standartları, teknoloji katmanı seçimini ve minimum token yükleme protokolünü tanımlar.

---

## 🏛️ 3 Katmanlı Mimari Sistemi

| Katman | Kapsam | Dosyalar | Yükleme Zorunluluğu |
|---|---|---|---|
| **Katman 0 — Evrensel** | Tüm projeler, tüm teknolojiler | `00_meta_agent_blueprint.md` `01_core_methodology.md` | Her oturumda zorunlu |
| **Katman 1 — Teknoloji** | Seçili platforma özel kurallar | `01_dotnet_standards.md` veya `01_flutter_standards.md` | Proje teknolojisine göre birini seç |
| **Katman 2 — Proje** | O projeye özel iş mantığı ve UI | `agent_guidelines.md` + `02` → `06` | Göreve göre 1-2 modül yükle |

---

## 📂 Katman 0: Evrensel Dosyalar (Her Oturumda Yükle)

1. 📐 **[00_meta_agent_blueprint.md](./00_meta_agent_blueprint.md)**
   — Proje iskeleti, MVVM klasör hiyerarşisi, 3 katmanlı bootstrap komutları ve otomatik kurulum anayasası.

2. 🔬 **[01_core_methodology.md](./01_core_methodology.md)**
   — Diff-only nokta atışı kodlama, token tasarrufu anayasası, otomatik kılavuz güncelleme zorunluluğu.

---

## 🔧 Katman 1: Teknoloji Dosyaları (Platforma Göre Birini Seç)

- 🟣 **[01_dotnet_standards.md](../platforms/dotnet/01_dotnet_standards.md)**
  — .NET WinForms / ASP.NET projeleri. DataGridView yasağı, GDI+ kart mimarisi, Task.Run async şablonu, Self-Contained deploy, MSB3021 kilit çözümü.

- 🔵 **[01_flutter_standards.md](../platforms/flutter/01_flutter_standards.md)**
  — Flutter Web projeleri. setState yasağı, Provider/ChangeNotifier state mimarisi, AuthGuardMixin, Syncfusion xlsio export, compute() isolate kuralı, `flutter build web` deploy standardı.

---

## 📁 Katman 2: Proje Modülleri (Göreve Göre 1-2 Modül Yükle)

Proje bazlı tam indeks ve modül açıklamaları için → **[agent_guidelines.md](../projects/DownTrackerApp/agent_guidelines.md)**

| Modül | İçerik |
|---|---|
| `02_industrial_data_rules.md` | İş mantığı, ham veri parse kuralları, alarm tarama algoritmaları |
| `03_ui_ux_standards.md` | Proje renk anayasası, tema sınırları, etkileşim kuralları |
| `04_performance_async.md` | Proje spesifik async yapılar ve Grafana sorgu motoru |
| `05_deployment_release.md` | Proje spesifik paketleme ve canlıya alma komutları |
| `06_troubleshooting_blacklist.md` | Sahada yaşanmış bug'lar, kök nedenler, kara liste |

---

## ⚡ Minimum Token Yükleme Protokolü

```
┌─ Her Oturum Başı (Zorunlu) ──────────────────────────────────────┐
│  00_meta_agent_blueprint  +  01_core_methodology                  │
│  +  01_[dotnet | flutter]_standards                               │
└───────────────────────────────────────────────────────────────────┘

┌─ Kod Geliştirme Görevi ──────────────────────────────────────────┐
│  Yukarıdakiler  +  ilgili 1 proje modülü (02 → 05 arası)         │
└───────────────────────────────────────────────────────────────────┘

┌─ Hata Ayıklama / Diagnostik Oturum ─────────────────────────────┐
│  Yukarıdakiler  +  06_troubleshooting_blacklist                   │
└───────────────────────────────────────────────────────────────────┘
```

---

## 🆕 Yeni Proje Kurulum Protokolü

Yeni bir proje başlatıldığında `00_meta_agent_blueprint.md` bootstrap komutu otomatik olarak:
1. MVVM klasör iskeletini fiziksel oluşturur (`Models/`, `ViewModels/`, `Views/`, `Assets/`, `Config/`).
2. Bu `agent_guidelines_universal.md` dosyasını `Config/universal/` altına kopyalar.
3. Projenin teknolojisine uygun Katman 1 dosyasını (`platforms/dotnet/` veya `platforms/flutter/`) üretir.
4. Proje katmanı boş şablonlarını (`projects/[ProjectName]/02` → `06` + `agent_guidelines.md`) oluşturur.
