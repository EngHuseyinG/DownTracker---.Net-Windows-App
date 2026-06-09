# AGENT BEHAVIOR & SYSTEM PROMPT (FLUTTER WEB TEKNOLOJİ KATMANI)
Sen Flutter Web tabanlı projelerde platform seviyesindeki widget mimarisi, state yönetimi, asenkron performans ve dağıtım standartlarını uygulayan teknoloji katmanı ajanısın. Bu dosyadaki kurallar, aynı platformun tüm projelerinde değişmez şekilde geçerlidir. Proje spesifik detaylar için `agent_guidelines.md` üzerinden ilgili proje modülünü yükle.

---

# T1. Flutter Web Platform Standartları (Teknoloji Katmanı)

## 🖼️ Flutter Widget Mimari Standartları

### 🛑 setState() Global Kullanım Yasağı
- **Kural:** Ağır veya geniş kapsamlı state değişikliklerini doğrudan `setState()` ile tetiklemek yasaktır.
- **Neden:** Gereksiz widget subtree rebuild'ine, UI jank'a ve veri tutarsızlığına neden olur.
- **Çözüm:** `Provider` paketi — `ChangeNotifier` + `notifyListeners()` ile state izolasyonu. `Selector<T,S>` ile yalnızca değişen alan rebuild edilir. `setState()` yalnızca yerel UI state için geçerlidir (`AnimationController`, `FocusNode`, `ScrollController`).

### 🔧 Widget Sorumluluk Ayrımı (Layer Rule)
- **StatelessWidget:** Salt UI render. İş mantığı, state veya async çağrı barındıramaz.
- **StatefulWidget:** Yalnızca yerel UI state (`AnimationController`, `FocusNode`, `ScrollController`). Veri çekme mantığı taşıyamaz.
- **ViewModel / Notifier:** Veri çekme, filtreleme, dönüştürme ve state yönetimi bu katmanda yaşar.

### 🛑 FutureBuilder İçi Future Üretim Yasağı
- **Kural:** `FutureBuilder`'ı `build()` metodu içinde inline `Future` üretecek şekilde kullanmak yasaktır.
- **Neden:** Her rebuild yeni bir `Future` başlatır; veri yanıp söner veya kaybolur.
- **Çözüm:**
```dart
// YANLIŞ:
FutureBuilder(future: fetchData(), ...)

// DOĞRU:
late final Future<Data> _dataFuture;

@override
void initState() {
  super.initState();
  _dataFuture = fetchData(); // Bir kez başlatılır
}

// build() içinde:
FutureBuilder(future: _dataFuture, ...)
```

### 🛑 ListView (Büyük Veri) Yasağı
- 20+ öğeli listelerde `ListView(children: [...])` veya `Column(children: [...])` kullanımı yasaktır.
- **Çözüm:** `ListView.builder` (lazy rendering) veya `CustomScrollView + SliverList`.

### 🔧 Web Responsive Breakpoint Standardı
```dart
// lib/core/constants/breakpoints.dart
class Breakpoints {
  static const double mobile  = 600;
  static const double tablet  = 1024;
  static const double desktop = 1280; // IIoT dashboard standardı
}

// Kullanım — kolon tabanlı grid (bkz. ## 📐 IIoT Dashboard Grid Kolon Standartları):
int _resolveColumns(double width) {
  if (width < Breakpoints.mobile)  return 1;
  if (width < Breakpoints.tablet)  return 2;
  if (width < Breakpoints.desktop) return 4;
  return 7; // Tam genişlik IIoT KPI dashboard
}

// childAspectRatio kolon sayısına göre dinamik — sabit değer yasaktır:
childAspectRatio: cols == 7 ? 1.2 : (cols == 4 ? 1.4 : 1.6),
```

## ⚡ Flutter Asenkron Performans Standartları

### 🔧 Loading State Şablonu (Evrensel)
```dart
// state enum — her ViewModel'de standart:
enum ViewState { idle, loading, success, error }

// Widget katmanında switch:
switch (viewState) {
  case ViewState.loading: return const LoadingOverlay();
  case ViewState.error:   return ErrorView(message: errorMessage);
  case ViewState.success: return DataView(data: data);
  default:                return const SizedBox.shrink();
}
```
`finally` bloğu state'i `idle`'a döndürmek için zorunludur; asla ihlal edilemez.

### 🔧 Ağır Hesaplama — compute() Kuralı
50MB+ veri işleme, JSON parse, kriptografi, görüntü işleme → `compute()` ile ayrı isolate'e devredilir:
```dart
final result = await compute(_parseJson, rawJsonString);
```
Main isolate'te senkron ağır işlem yasaktır.

> ⚠️ **Kapsam Notu:** RTDB kayıtları için client-side pagination yeterliyse `compute()` gerekmez. Yalnızca 50MB+ raw binary veri, görüntü işleme veya kriptografi gibi gerçek CPU-bound görevlerde devreye girer. Firebase sorgu sonuçlarına `compute()` uygulamak gereksiz isolate overhead'i yaratır.

### 🔧 async/await Zinciri Kuralı
- `Future.wait` ile paralel çağrılar tercih edilir.
- `then()` zincirleme karmaşıklığı yasaktır; `async/await` okunabilirliği zorunludur.

## 🌐 Flutter Web Deployment Standartları

### Build Komutu (Evrensel Flutter Web)
```bash
flutter build web --release --web-renderer canvaskit
```

### Firebase Hosting Deploy
```bash
firebase deploy --only hosting
```

### pubspec.yaml Versiyon Zorunluluğu
```yaml
version: 1.0.0+1  # Major.Minor.Patch+buildNumber — her release'de artır
environment:
  sdk: '>=3.0.0 <4.0.0'
```

### PWA Yapılandırması
- `web/manifest.json` içinde `"display": "standalone"` zorunludur.
- Service Worker cache stratejisi `NetworkFirst` (online-first) önerilir.

## 🐛 Flutter Web Platform Kara Listesi

| # | Yasaklı Yapı | Platform Riski | Zorunlu Çözüm |
|---|---|---|---|
| 1 | `setState()` (global/ağır) | Gereksiz full widget rebuild | `ChangeNotifier` + `notifyListeners()` — `Selector` ile rebuild izolasyonu |
| 2 | `FutureBuilder` (build içi inline Future) | Her rebuild'de yeni Future | `initState()` içinde ata |
| 3 | `ListView(children: [...])` (büyük liste) | O(n) eager render, UI donması | `ListView.builder` (lazy) |
| 4 | Main isolate'te sync hesaplama | UI jank / frame drop | `compute()` / `Isolate.run()` |
| 5 | `print()` (production kodu) | Performans düşüşü, log sızıntısı | `debugPrint()` veya `logger` paketi |
| 6 | `then()` zincirleme (karmaşık) | Okunaksız hata yönetimi | `async/await` + `try/catch` |
| 7 | `context` (async gap sonrası) | `BuildContext` stale referansı | `mounted` kontrolü zorunlu |
| 8 | `context.read<T>()` inside `build()` | Stale snapshot, anında stale okuma | Yalnızca handler / `initState` içinde |
| 9 | Sıralı `await` döngüsü (multi-source fetch) | N×RTT gecikme, UI donması | `Future.wait([...])` paralel çekme |
| 10 | `AuthGuardMixin` olmadan içerik sayfası | Kimliksiz erişim açığı | `AuthGuardMixin` zorunlu |
| 11 | Web export için `path_provider` | Web'de desteklenmez, crash | `dart:html` Blob + `AnchorElement` |

---

## 🔌 Provider Package State Management Standartları

> Bu bölüm `provider: ^6.x` paketi kullanan projeler için geçerlidir (Riverpod değil).

### 🛑 `Consumer` / `context.watch()` Geniş Kapsamlı Kullanım Yasağı
- **Kural:** Büyük subtree'lerin tepesinde `context.watch<T>()` veya sarmalayıcı `Consumer<T>()` kullanmak yasaktır.
- **Neden:** Her `notifyListeners()` bağlı tüm subtree'yi rebuild eder; FPS düşüşü ve UI jank oluşur.
- **Çözüm:** `Selector<T, S>` ile yalnızca değişen alan izlenir; `Consumer<T>` sadece leaf widget'larda kullanılır.

```dart
// YANLIŞ — tüm sayfayı rebuild eder:
Consumer<MachineVM>(
  builder: (ctx, vm, _) => Scaffold(body: MachineBody(vm)),
)

// DOĞRU — sadece ilgili alanı dinler:
Selector<MachineVM, String>(
  selector: (_, vm) => vm.connectionStatus,
  builder: (ctx, status, _) => StatusBadge(status),
)
```

### 🔧 `context.read` vs `context.watch` Ayrım Kuralı
```dart
// ✅ Yalnızca event handler ve initState içinde:
context.read<MachineVM>().loadData();

// ✅ Yalnızca build() içinde, mümkün olan en küçük subtree'de:
final status = context.watch<MachineVM>().status;
```
- `context.read()` → **asla** `build()` içinde çağrılmaz (stale snapshot riski).
- `context.watch()` → **asla** handler içinde çağrılmaz (gereksiz re-subscribe).

### 🔧 `resetPage()` on `initState` Standardı
Her navigation sonrası stale data gösterimini önlemek için:
```dart
@override
void initState() {
  super.initState();
  WidgetsBinding.instance.addPostFrameCallback((_) {
    context.read<PageVM>().resetPage();
  });
}
```
- `resetPage()` ViewModel'de state'i initial değerlerine döndürür, her sayfada zorunludur.
- `addPostFrameCallback` ile çağrılır — provider dependency grafiğindeki sıralama sorunlarını önler.

### 🔧 Global Provider Enjeksiyon Standardı
```dart
MultiProvider(
  providers: [
    ChangeNotifierProvider(create: (_) => AuthProvider()),   // kimlik state'i
    Provider(create: (_) => FirebaseService()),              // immutable servis
    ChangeNotifierProvider(create: (_) => DrawerVM()),       // layout state
  ],
  child: MaterialApp(...),
)
```
- State içermeyen servisler → `Provider<T>` (ChangeNotifier değil).
- Sayfa ViewModel'leri kök'e değil, ilgili route ağacına enjekte edilir.

---

## 🔥 Firebase RTDB Async Standartları

### 🔧 AuthGuardMixin + 1500ms Sync Delay Standardı
Firebase Auth token yenileme süresiyle oluşan race condition'ı önlemek için:
```dart
mixin AuthGuardMixin<T extends StatefulWidget> on State<T> {
  @override
  void initState() {
    super.initState();
    Future.delayed(const Duration(milliseconds: 1500), _checkAuth);
  }

  void _checkAuth() {
    if (!mounted) return;
    if (!context.read<AuthProvider>().isAuthenticated) {
      Navigator.pushReplacementNamed(context, '/login');
    }
  }
}
```
- `/login` hariç **tüm içerik sayfaları** `AuthGuardMixin` ile korunmak zorundadır.
- 1500ms delay kaldırılamaz — Firebase token yenileme gecikmesinden kaynaklanan hard constraint.

### 🔧 Multi-Source RTDB Fetching Standardı
```dart
Future<List<TestRecord>> fetchMultiSource(
    List<String> deviceIds, String dateKey) async {
  // Paralel çekme — sıralı await döngüsü yasaktır
  final snapshots = await Future.wait(
    deviceIds.map((id) => _db.ref('testresults/$id/$dateKey').get()),
  );
  return snapshots
      .where((s) => s.exists)
      .expand((s) => _parseSnapshot(s))
      .toList();
}
```

### 🔧 Client-Side Pagination Standardı (1000+ Kayıt)
```dart
int _page = 0;
int _pageSize = 25; // [10, 25, 50, 100] seçenekleri

List<TestRecord> get paginatedData =>
    _allRecords.skip(_page * _pageSize).take(_pageSize).toList();
```
- `_allRecords` private tutulur; UI yalnızca `paginatedData` getter'ına erişir.
- 60FPS korunması için `paginatedData` hesaplaması `notifyListeners()` döngüsünün dışında kalır.

### 🔧 RTDB Log Kronolojik Sıralama Standardı
YYMMDD + HHmmss string key'leri — String karşılaştırması yasaktır:
```dart
records.sort((a, b) {
  final tA = int.parse(a.dateKey + a.timeKey); // "240615" + "143022"
  final tB = int.parse(b.dateKey + b.timeKey);
  return tB.compareTo(tA); // Descending — en yeni üstte
});
```

---

## 📊 Syncfusion xlsio Export Standartları

### 🔧 Flutter Web Universal Download Şablonu
```dart
import 'dart:html' as html;
import 'package:flutter/foundation.dart' show kIsWeb;

Future<void> downloadExcel(List<int> bytes, String fileName) async {
  if (kIsWeb) {
    final blob = html.Blob(
      [Uint8List.fromList(bytes)],
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    );
    final url = html.Url.createObjectUrlFromBlob(blob);
    html.AnchorElement(href: url)
      ..setAttribute('download', fileName)
      ..click();
    html.Url.revokeObjectUrl(url); // Bellek sızıntısını önler — zorunlu
  } else {
    final dir = await getApplicationDocumentsDirectory();
    final file = File('${dir.path}/$fileName');
    await file.writeAsBytes(bytes);
    await OpenFile.open(file.path);
  }
}
```
- `dart:html` import → `kIsWeb` guard olmadan kullanılamaz (non-web build bozulur).
- `revokeObjectUrl` unutulmaması zorunludur — Blob URL memory leak'e neden olur.

### 🔧 Kurumsal Hücre Stili Standartları
```dart
// Standart sütun genişliği — A'dan Z'ye:
for (var i = 1; i <= 26; i++) sheet.getRangeByIndex(1, i).columnWidth = 10;

// Başlık satırı:
sheet.getRangeByName('B1').cellStyle
  ..bold = true
  ..fontSize = 12
  ..backColorRgb = const Color(0xFF1A237E)
  ..fontColorRgb = Colors.white;

// Logo embed — B3 anchor (row=3, col=2):
sheet.pictures.addBase64(3, 2, base64LogoString)
  ..height = 40
  ..width = 120;

// Filtre başlıkları — 2 sütun merge:
sheet.getRangeByName('F2:G2')
  ..merge()
  ..cellStyle.hAlign = HAlignType.center;

// Tüm data range 4-taraf ince border + center:
final range = sheet.getRangeByName('B5:Z$lastRow');
range.cellStyle.borders.all.lineStyle = LineStyle.thin;
range.cellStyle.hAlign = HAlignType.center;

// Alternatif satır rengi:
for (var r = 6; r <= lastRow; r++) {
  if (r.isEven) {
    sheet.getRangeByIndex(r, 2, r, lastCol)
        .cellStyle.backColorRgb = const Color(0xFFF5F5F5);
  }
}
```

### 🔧 Chart Data Label Standardı
```dart
dataLabel.isVisible = true;
dataLabel.textStyle = const ChartTextStyle(
  color: Colors.white,
  fontWeight: FontWeight.bold,
  fontSize: 10, // sabit — değiştirilemez
);

// Pie/Doughnut — en büyük segment explode:
series.explode = true;
series.explodeIndex = dominantSegmentIndex;
```

---

## 📐 IIoT Dashboard Grid Kolon Standartları

### 🔧 Çok Kolonlu KPI Grid Pattern
```dart
int _resolveColumns(double width) {
  if (width < 600)  return 1;
  if (width < 1024) return 2;
  if (width < 1280) return 4;
  return 7; // Tam genişlik dashboard — IIoT KPI panelleri
}

GridView.builder(
  gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
    crossAxisCount: cols,
    crossAxisSpacing: 12,
    mainAxisSpacing: 12,
    childAspectRatio: cols == 7 ? 1.2 : (cols == 4 ? 1.4 : 1.6),
  ),
  ...
)
```
- 7 kolon → yalnızca `>= 1280px` wide desktop (IIoT KPI dashboard'ları).
- `childAspectRatio` sabit değer yasaktır; kolon sayısına göre dinamik ayarlanır.

---

## 🔒 Güvenlik ve Yetkilendirme Standartları

### 🔧 Seviye Tabanlı Yetki Matrisi
```dart
// AuthProvider içinde:
// 1: Viewer  |  2: MachineManager  |  3: WifiManager  |  4+: Admin
bool get canManageMachines => userLevel >= 2;
bool get canManageWifi     => userLevel >= 3;

// Widget katmanında guard:
if (!context.read<AuthProvider>().canManageMachines)
  return const UnauthorizedView();
```

### 🛑 AuthGuardMixin Zorunluluğu
- `/login` hariç her route için `AuthGuardMixin` zorunludur.
- Guard bypass edilemez; `Navigator.pushNamed` ile doğrudan route erişimi `AuthGuardMixin` tarafından kesilir.

---

## ⚙️ IIoT PLC Veri Dönüşüm Standartları

### 🔧 HMI Scaling Formülü (Digits / Fractional)
```dart
// Firebase machinesettings'den gelen parametreler:
// Fractional: ondalık basamak sayısı (ölçekleme faktörü)
double applyHmiScaling(int rawValue, int fractional) =>
    rawValue / pow(10, fractional);

// Precision formatting:
String formatEngValue(double value, int fractional) =>
    value.toStringAsFixed(fractional); // fractional=2 → "1.50"
```
- Birim standardı: **mbar** — kaçak ölçüm değerlerinde asla başka birim kullanılmaz.

### 🔧 16-bit → 32-bit PLC Register Rekonstruksiyon Standartları
```dart
// Modbus High/Low Word birleştirme:
int reconstruct32bit(int highWord, int lowWord) =>
    (highWord << 16) | (lowWord & 0xFFFF);

// IEEE 754 Float (PLC Real tipi) dönüşümü:
double toIeee754Float(int highWord, int lowWord) {
  final bits = reconstruct32bit(highWord, lowWord);
  return (ByteData(4)..setUint32(0, bits, Endian.big))
      .getFloat32(0, Endian.big);
}
```
- PLC `Real` tipi → her zaman IEEE 754 dönüşümü yapılır; integer casting yasaktır.
