# AGENT BEHAVIOR & SYSTEM PROMPT
Sen DownTracker'ın saha diagnostic ve kriz yönetimi uzmanısın. Projede yeni bir özellik geliştirirken veya hata ayıklarken, bu dosyada belirtilen 'Yasaklılar (Blacklist)' listesindeki mimari hataları asla tekrarlayamazsın. Yazacağın kodlar, geçmişte yaşanmış asenkron ve donanımsal kilitlenme tecrübelerine tam uyumlu olmak zorundadır.

---

# 6. Saha Hata Kataloğu, Kara Liste ve Çözümler (Troubleshooting)

Bu modül, Ford Otosan üretim hattından gelen gerçek zamanlı Kepware loglarının işlenmesi ve WinForms platformunun kararsızlıkları esnasında karşılaşılan sinsi bug'ların kök nedenlerini ve bir daha asla kullanılmaması gereken kod standartlarını içerir.

## 🛑 KOD VE MİMARİ KARA LİSTE (BLACKLIST)

### 1. Grid Odaklanma ve Renk Dayatması Yasağı (`uxtheme.dll`)
- **Yasaklı Yapı:** Detay veya pop-up ekranlarında hücre renklendirmek amacıyla `DataGridView` bileşeni kullanmak kesinlikle yasaktır.
- **Kök Neden:** Windows tema motoru (`uxtheme.dll`), aktif odaklanan satırlara kendi donuk zeytin yeşili/kirli sarı rengini zorla dayatır ve C# tarafındaki `CellFormatting` veya `RowPrePaint` ezmelerini tamamen bypass ederek arayüz görsel hiyerarşisini bozar.
- **Nihai Çözüm:** `%100 Saf C# Dinamik Kart Mimarisi` (`FlowLayoutPanel` ve dinamik `Panel` nesneleri). Tüm çizim ve vurgulamalar saf GDI+ fırçalarıyla (Pure Code-Behind) yönetilir.

### 2. Arayüz Temizleme Katliamı Yasağı (`Controls.Clear()`)
- **Yasaklı Yapı:** Panel veya sekme içeriklerini yenilemek için `panel.Controls.Clear()` veya `Form.Controls.Clear()` çağrısı yapmak kesinlikle yasaktır!
- **Kök Neden:** WinForms mimarisinde `Controls.Clear()` çağrısı, tasarım anında (Design-time) forma eklenmiş olan ve arka planda yaşamaya devam etmesi gereken `tabMain` gibi sarmalayıcı nesneleri hafızadan tamamen siler (`Disposed` durumuna düşürür). Bu durum uygulamanın anında çökmesine ve sekmelerin kalıcı olarak yok olmasına neden olur.
- **Nihai Çözüm:** Kontroller hiyerarşik olarak eklenmeli, sadece dinamik üretilen kart listeleri temizlenmeli veya kontroller `BringToFront()` / `SendToBack()` metotlarıyla önbellekte yönetilmelidir.

### 3. Ağır İşlemlerde UI Thread İşgali Yasağı (Synchronous IO)
- **Yasaklı Yapı:** CSV/Excel okuma, satır satır parsing, regex eşleştirmeleri ve istasyon izolasyon algoritmalarını doğrudan buton click olayları veya ana thread altında senkron çalıştırmak kesinlikle yasaktır.
- **Kök Neden:** Büyük endüstriyel log dosyaları (milyonlarca satır) işlenirken ana arayüz iş parçacığı (UI Thread) kilitlenir, Windows arayüzü beyazlaşır ve işletim sistemi işletmeyi "Yanıt Vermiyor" olarak işaretleyip çökertir.
- **Nihai Çözüm:** Tüm parsing ve ağır algoritma motoru `Task.Run(() => { ... })` ile `Background Thread`'e devredilmeli, `async/await` asenkron köprüsü kurulmalıdır.

---

## 🐛 Sahada Yaşanmış Kritik Buglar ve Çözüm Kodları

### 1. Ana İstasyon Asenkron Alarm ve Propagation Delay Hatası
- **Sorun:** Robot arızaya geçtiğinde (`9A30LH-R1...ST_DownCondition = True`), gerçek hata alarm kodunu (`DiscreteAlarm5`) ana istasyondan 1.4 saniye önce ateşlemektedir. Ana istasyon (`9A30LH.-`) duruş sinyalini aldığında tam o saniyeye baktığı için alarmı ıskalamakta ve ekrana **'Boş Arıza' (Boş Alarm)** basmaktadır.
- **Çözüm Algoritması:** Arıza durumlarının kök neden alarmları, saniye veya süreden tamamen bağımsız olarak, sadece ve sadece o arızanın içinde gerçekleştiği iki TransactionEnd = True (üretim çevrimi) sınırları içerisinde aranır.

```csharp
// Çözüm Mantığı - Models/ParsingEngine.cs
LogEntry GetMatchingAlarm(string rName, DateTime dStart, DateTime dEnd, Cycle currentCycle)
{
    if (string.IsNullOrEmpty(rName))
    {
        // Ana istasyon: Duruşun ait olduğu iki TransactionEnd arasındaki tüm robot/istasyon alarmlarını tarar
        return stationLogs
            .Where(al => al.SignalType == "DiscreteAlarm" &&
                         al.Value &&
                         al.TagDate >= currentCycle.StartTime &&
                         al.TagDate <= currentCycle.EndTime &&
                         al.TagName.Contains(stationName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(al => al.TagDate)
            .FirstOrDefault();
    }
    else
    {
        // Robot: Kendi çevrim sınırları içindeki tam eşleşme mantığı korunur
        return stationLogs
            .Where(al => al.SignalType == "DiscreteAlarm" &&
                         al.Value &&
                         al.RobotName == rName &&
                         al.TagDate >= currentCycle.StartTime &&
                         al.TagDate <= currentCycle.EndTime)
            .OrderBy(al => al.TagDate)
            .FirstOrDefault();
    }
}
```

### 2. UI/UX Etkileşim Çakışması ve Ekran Sağırlığı Hatası
- **Sorun:** Zaman çizelgesi üzerindeki `MouseMove` (Hover) olayları ile alttaki kart tıklama (`Click`) olayları tek bir merkezi paneli (`lblCentralSignalInfo`) beslediğinde veri kirliliği ve çakışma yaşanmıştır. Hover temizlendiğinde kullanıcının tablodan seçtiği kalıcı veri ekrandan silinmektedir.
- **Çözüm Algoritması:** Rollerin kesin ayrımı (Hybrid UX) yapılmıştır. Tepe paneli yalnızca fare üzerindeyken çalışır ve fare boşluğa çıktığı an varsayılan metne döner. Kart tıklamaları bu paneli kesinlikle etkilemez; kartlar kendi içinde 55px dikey eksende esneyerek upuzun Kepware tag yollarını Wrap modunda kendi gövdesinde gösterir.

### 3. TreeView Daraltma (CollapseAll) Sonrası Form Sağırlaşması
- **Sorun:** Sol ağaç yapısında `CollapseAll()` çağrısı yapıldıktan sonra WinForms durum makinesi kilitlenmekte ve "Tüm Varlıklar" filtresi tetiklendiğinde ağaç tepki vermemektedir.
- **Çözüm Algoritması:** Seçim önce tamamen boşa düşürülmeli, ardından manuel olarak kök düğüme odaklanılmalıdır:

```csharp
tvStations.SelectedNode = null;
tvStations.SelectedNode = tvStations.Nodes[0];
tvStations.Focus();
```

### 4. GDI+ Bilgi Etiketi (Label) Genişlik Sınırı ve Kırpılma Hatası
- **Sorun:** Detay ekranında zaman çizelgesi üzerinde gezinirken (`MouseMove`), `lblCentralSignalInfo` etiketinde uzun Kepware sinyal tag yolları gösterilmek istendiğinde metnin sonu `| Sinyal: ` şeklinde kalıp asıl Kepware tag adı ekranda görünmemekteydi.
- **Kök Neden:** WinForms'ta `Label` bileşeni `Dock = DockStyle.Fill` ve `AutoSize = false` modundayken, eğer parent panel yüksekliği (`45px`) ve etiket yazı boyutu (`9.5F Bold`) nedeniyle iki satırlık alan (~36px + padding) yetersiz kalırsa veya dikey hizalama nedeniyle metin sığmazsa, otomatik sarılan ikinci satır (sinyal adını içeren kısım) görsel olarak tamamen kırpılmaktaydı.
- **Çözüm Algoritması:** Parent panel yüksekliği `55px` değerine çıkarılmış, yazı tipi boyutu `9F` seviyesine çekilerek dikeyde yeterli alan kazanılmış ve kırpılma tamamen önlenmiştir.

### 5. Derleme Sırasında Binary Kilitlenmesi (MSB3021 / MSB3027 Hatası)
- **Sorun:** Proje üzerinde değişiklik yapıldıktan sonra `dotnet build` komutu çalıştırıldığında, `apphost.exe` dosyasının `bin/Debug/net10.0-windows/win-x64/DownTracker.exe` dosyası üzerine kopyalanamaması sonucu derleme yarıda kalır.
- **Kök Neden:** Uygulamanın bir kopyası arka planda veya debug modunda hala aktif olarak çalışmakta ve derlenecek olan executable binary dosyasını işletim sistemi düzeyinde kilitlemektedir.
- **Çözüm Algoritması:** Derleme başlatılmadan önce çalışan tüm `DownTracker.exe` işlemleri zorla sonlandırılmalıdır:
  ```powershell
  taskkill /IM DownTracker.exe /F
  ```

### 6. Hatalı Import Ekranında Grid Satırlarının Kırpılması (Custom Scrollbar Çakışması)
- **Sorun:** P360 Hatalı Import Sihirbazı ekranında (`FaultyImportView`), 2. Örnek Excel Grid'inde (Grafana input) ikinci satır verileri görünmez oluyordu.
- **Kök Neden:** `BindCustomScrollbar` yardımcı metodunun, sarmalayıcı panelin yüksekliğini (`containerPanel.Height = gridHeight + 10`) şeklinde ezmesi sonucu gridin başlık ve padding payları hesaba katılmayarak panel daralıyor ve grid satırları kırpılıyordu.
- **Çözüm Algoritması:**
  1. `BindCustomScrollbar` metodunun sarmalayıcı panelin yüksekliğini ezmesi engellenmiş, panelin tasarım anındaki veya atanmış asil yüksekliği (`170px`, `135px`, `115px`) korunarak yatay scrollbar panelin en altına mühürlenmiştir (`DockStyle.Bottom`). Bu sayede kaydırma çubuğu grid satırlarının üzerine binmeden en altta render edilir ve veri satırları kırpılmaz.
  2. Kaydırma çubuğunun en alttaki Excel yükleme butonu sarmalayıcısı (`pnlDragContainer`) ile üst üste binmesini önlemek amacıyla, `pnlDragContainer` kontrolünün docking tipi `Dock = DockStyle.Top` yapılmış ve Z-Order sıralamasında en alta gönderilmiştir (`SendToBack`). Bu sayede tüm arayüz dikeyde sıralı bir şekilde yerleşir ve sayfa boyutu küçüldüğünde dikey kaydırma çubuğu (AutoScroll) devreye girerek elemanların birbirini örtmesini engeller.
