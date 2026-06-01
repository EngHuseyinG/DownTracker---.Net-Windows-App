# 📊 DownTracker - Industrial Production Cycle & Downtime Analyzer

DownTracker, Kepware ve endüstriyel otomasyon sistemlerinden (PLC/SCADA) gelen ham log verilerini (CSV formatında) milisaniye hassasiyetiyle işleyen, istasyon ve robot bazlı **Üretim Çevrimi (Production Cycle)** ve **Mikro-Duruş / Kök Neden (Downtime & Alarm Root Cause)** analizi yapan kurumsal düzeyde bir diagnostic (tanılama) yazılımıdır.

Proje, geleneksel hantal masaüstü arayüzlerini yıkarak, tamamen **Pure Code-Behind** ile inşa edilmiş dinamik kart listeleri ve senkronize GDI+ zaman çizelgesi (Timeline Canvas) ile modern bir SCADA/Grafana deneyimi sunar.

---

## 🏗️ 1. Öne Çıkan Özellikler & UI/UX Mimarisi

* **%100 Saf C# Dinamik Kart Mimarisi:** Standart WinForms `DataGridView` bileşenlerinin pop-up ekranlarda yarattığı Windows tema motoru (`uxtheme.dll`) kilitlenmelerini ve seçim odak (Focus) sorunlarını kökten çözmek için tablo yapısı tamamen terk edilmiştir. Yerine, işletim sistemi müdahalelerinden izole, dinamik esneyen kart listeleri (`FlowLayoutPanel`) kurulmuştur.
* **Çift Yönlü Görsel Senkronizasyon (Cross-Highlighting):** Alttaki listeden herhangi bir arıza kartına tıklandığında, kart dikey eksende `35px` değerinden `55px` değerine esneyerek uzun Kepware etiket yollarını kırpmadan (`Wrap` metin modunda) gösterir. Aynı milisaniyede, üstteki GDI+ zaman çizelgesindeki izdüşümü canlı neon pembe (`#FF0055`) renkle parlar.
* **Merkezi Sinyal Takip Paneli (Hybrid UX):** Fare zaman çizelgesindeki kalınlaştırılmış (3px) neon sarı alarm çizgilerinin veya kırmızı duruş şeritlerinin üzerinde gezdirilirken (`MouseMove`), ilgili Kepware sinyalinin başlangıç, bitiş, süre ve tag path künyesi en tepedeki panelde anlık altyazı olarak akar. Fare çekildiğinde panel sıfırlanır; kart tıklamaları ise bu paneli kirletmez.
* **Otomatik Grafana SQL Sorgu Motoru:** Ekrandaki filtrelerin (`Tarih`, `Kanal`, `Asset`) anlık olaylarına (`TextChanged` / `CheckedChanged`) bağlı olarak arka planda dinamik, ilişkisel Grafana SQL sorguları el değmeden üretilir.

---

## 🔒 2. Katı Endüstriyel İş Mantığı & Algoritma Kuralları

Yazılım, sahadaki veri kirliliğini (data contamination) önlemek amacıyla şu katı kurallarla çalışır:

1.  **Sinyal Sonu & Ortak Çevrim Referansı:** Robot assetlerinin kendilerine özel `TransactionEnd` sinyali yoktur; bağlı oldukları üst istasyonun meşru ortak `.Outputs.Transactionend` (True) sinyalini ortak üretim penceresi referansı olarak alırlar.
2.  **Nesne Tabanlı %100 İstasyon İzolasyonu:** Filtreleme işlemlerinde string araması (`.Contains`) yerine katı nesne eşitliği kullanılır. İsmi açıkça o istasyonla birebir eşleşmeyen hiçbir log veya duruş satırı zamanı ne olursa olsun o istasyonun paneline sızamaz, bloke edilir.
3.  **Arızanın Çevrimle Kesilmesi (Downtime Splitting):** Devam eden bir arıza varken araya yeni bir üretim çevrimi (`TransactionEnd = True`) girerse, arıza o noktada matematiksel olarak bölünür. İlk parça o çevrimdeki ilk alarmı kök neden alır, ikinci parça ise yeni çevrimde gerçek bitiş sinyaliyle sonlanır.

---

## 📂 3. Klasör ve Mimari Yapısı

Proje, WinForms platformu üzerinde **MVVM (Model-View-ViewModel)** tasarım deseniyle katı bir klasör hiyerarşisinde geliştirilmiştir:

```text
DownTrackerApp/
│
├── Models/          # Ham veri yapıları, CSV Log nesneleri ve Veri Modelleri
├── ViewModels/      # Filtreleme, SQL sorgu motoru ve katı analiz algoritmaları
├── Views/           # Ana Ekran ve GDI+ Çizimli FormDowntimeInfo Detay Formu
└── Config/          # Proje anayasası ve agent_guidelines geliştirici kılavuzu
