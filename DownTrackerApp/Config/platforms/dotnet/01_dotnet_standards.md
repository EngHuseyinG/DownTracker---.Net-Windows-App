# AGENT BEHAVIOR & SYSTEM PROMPT (.NET TEKNOLOJİ KATMANI)
Sen .NET tabanlı (WinForms / ASP.NET) projelerde platform seviyesindeki UI mimari, asenkron performans ve dağıtım standartlarını uygulayan teknoloji katmanı ajanısın. Bu dosyadaki kurallar, aynı platformun tüm projelerinde değişmez şekilde geçerlidir. Proje spesifik detaylar için `agent_guidelines.md` üzerinden ilgili proje modülünü yükle.

---

# T1. .NET Platform Standartları (Teknoloji Katmanı)

## 🖼️ WinForms UI Mimari Standartları

### 🛑 DataGridView + uxtheme.dll Yasağı
- **Kural:** Detay ve pop-up ekranlarında `DataGridView` kullanımı kesinlikle yasaktır.
- **Neden:** Windows tema motoru (`uxtheme.dll`), odaklanan satırlara zeytin yeşili/kirli sarı renk zorla dayatır; C# tarafındaki `CellFormatting` ve `RowPrePaint` ezmeleri tamamen bypass edilir.
- **Çözüm:** `%100 Saf C# Dinamik Kart Mimarisi` — `FlowLayoutPanel` + runtime `Panel` nesneleri. Tüm renk ve vurgulamalar saf GDI+ fırçalarıyla yönetilir.

### 🛑 Controls.Clear() Yasağı
- **Kural:** Panel veya Form üzerinde `Controls.Clear()` çağrısı kesinlikle yasaktır.
- **Neden:** WinForms Design-time sarmalayıcı nesneleri (`TabControl`, `SplitContainer` vb.) `Disposed` durumuna düşer, uygulama çöker, sekmeler kalıcı olarak kaybolur.
- **Çözüm:** Kontroller hiyerarşik eklenmeli; dinamik üretilen kart listeleri tek tek `Remove()` ile temizlenmeli; görünürlük `BringToFront()` / `SendToBack()` ile yönetilmelidir.

### 🔧 TreeView CollapseAll Odak Kilidi Çözümü
`CollapseAll()` sonrası WinForms durum makinesinin kilitlenmesini önlemek için:
```csharp
treeView.SelectedNode = null;
treeView.SelectedNode = treeView.Nodes[0];
treeView.Focus();
```

### 🔧 Z-Order ve Dock Hizalama Kuralı
- `Dock = DockStyle.Fill` olan ana kontrol (`SplitContainer`, `TabControl`) her zaman `BringToFront()` ile Z-Order tepesinde tutulur.
- Kenar paneller (`StatusStrip`, dış sınır panelleri) `SendToBack()` edilir.

### 🔧 Tab İkonu — GDI+ Vektörel Çizim Zorunluluğu
- Font tabanlı emoji/karakter ikonları sekme başlıklarında `□` boş kutu render hatasına neden olur.
- Zorunlu çözüm: Runtime `CreateImageList()` ile GDI+ vektörel çizim motoru. Font tabanlı ikon kullanımı yasaktır.

## ⚡ .NET Asenkron Performans Standartları

### 🛑 UI Thread Kilitleme Yasağı
- Dosya okuma, CSV/Excel parsing, DB sorgusu, ağır algoritma → kesinlikle `Task.Run` arka plan thread'ine devredilir.
- Ana thread'de `await` ile beklenir; asla `Task.Result` veya `.Wait()` çağrılmaz (deadlock riski).

### 🔧 Loading Overlay Mühürlü Şablonu
```csharp
overlayPanel.Visible = true;
try
{
    await Task.Run(() =>
    {
        // Ağır iş burada (parsing, IO, algoritma)
    });
}
catch (Exception ex)
{
    // Hata yönetimi — log veya kullanıcıya bildir
}
finally
{
    this.Invoke(() => overlayPanel.Visible = false); // finally zorunludur
}
```
`finally` bloğu overlay kapatma için zorunlu mühürlü yapıdır; asla ihlal edilemez.

### 🔧 Cross-Thread UI Güncelleme Kuralı
Arka plan thread'inden UI kontrollerine erişim yalnızca `this.Invoke()` veya `control.BeginInvoke()` ile yapılır:
```csharp
this.Invoke(() => lblStatus.Text = "Tamamlandı.");
```

## 📦 .NET Self-Contained Deployment Standartları

### .csproj Zorunlu Parametreler
```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishReadyToRun>true</PublishReadyToRun>
```

### Deployment Komutu (Evrensel .NET)
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### Binary Kilit Çözümü (MSB3021 / MSB3027)
Derleme başlatılmadan önce çalışan exe sürecini zorla kapat:
```powershell
taskkill /IM [AppName].exe /F
```

## 🐛 .NET / WinForms Platform Kara Listesi

| # | Yasaklı Yapı | Platform Riski | Zorunlu Çözüm |
|---|---|---|---|
| 1 | `DataGridView` (detay/pop-up) | uxtheme.dll renk dayatması | `FlowLayoutPanel` + GDI+ kartlar |
| 2 | `Controls.Clear()` | Disposed çöküş, sekme kaybı | `BringToFront` / `SendToBack` |
| 3 | Sync IO (UI Thread üzeri) | UI freeze, "Yanıt Vermiyor" | `Task.Run` + `async/await` |
| 4 | Font emoji tab ikonları | `□` boş kutu GDI+ render hatası | Runtime `CreateImageList()` |
| 5 | `Task.Result` / `.Wait()` | Deadlock (özellikle WinForms pump) | `async/await` zinciri |
| 6 | Derleme öncesi aktif exe | MSB3021 binary kilit | `taskkill /IM [App].exe /F` |
| 7 | UI kontrol erişimi (arka plan) | Cross-thread exception | `this.Invoke()` sarmalayıcısı |

> **📌 Proje Bazlı Bug Kataloğu:** Platform kurallarının gerçek saha örnekleri,
> çözüm kod blokları ve proje spesifik kara liste için:
> → `../../projects/[ProjectName]/06_troubleshooting_blacklist.md`

---

## 🖥️ Konsol Uygulaması Standartları

### 🔧 WinForms Kurallarının İstisnası
Bu proje bir Konsol uygulamasıdır. Aşağıdaki WinForms'a özel kurallar bu projede **GEÇERSİZDİR:**
- `pnlLoadingOverlay` → Yok; ilerleme `Console.Write` ile raporlanır
- `this.Invoke()` → Yok; cross-thread erişim sorunu yoktur
- `Controls.Clear()` yasağı → Yok; WinForms kontrolü yoktur
- `DataGridView` yasağı → Yok; UI bileşeni yoktur

### 🔧 Konsol Async Standardı
```csharp
static async Task Main(string[] args)
{
    try
    {
        await RunAsync();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[HATA] {ex.Message}");
        Environment.Exit(1);
    }
}
```
- `Console.WriteLine` → yalnızca debug/geliştirme
- Production log → `Serilog` veya `Microsoft.Extensions.Logging`
- `CancellationToken` → tüm async metodlara parametre olarak geçilir

---

## ⚙️ Windows Backend Servis Standartları

### 🔧 WinForms Kurallarının İstisnası
Bu proje bir Windows Servisidir. WinForms'a özel tüm kurallar (Overlay, Invoke, DataGridView, Controls) bu projede **GEÇERSİZDİR.**

### 🔧 Servis Yaşam Döngüsü Standardı
```csharp
public class WorkerService : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Servis döngüsü hatası");
                await Task.Delay(5000, stoppingToken); // retry delay
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
```
- `BackgroundService` → tüm servisler bu base class'tan türer
- `stoppingToken` → her async çağrıya geçilir; asla görmezden gelinmez
- Exception swallow yasaktır → loglama zorunludur

### 🔧 Servis Deployment Komutu
```powershell
# Kur:
sc create [ServiceName] binPath="[ExePath]" start=auto
# Başlat:
sc start [ServiceName]
# Kaldır:
sc delete [ServiceName]
```

### 🐛 Servis Kara Listesi
| # | Yasaklı Yapı | Risk | Çözüm |
|---|---|---|---|
| 1 | `Thread.Sleep()` | Token iptalini engeller | `Task.Delay(ms, token)` |
| 2 | Exception swallow | Sessiz servis çöküşü | Log + retry delay |
| 3 | Static mutable state | Race condition | DI singleton + lock |
| 4 | `Console.WriteLine` | Servis loglarına ulaşmaz | `ILogger` zorunlu |
