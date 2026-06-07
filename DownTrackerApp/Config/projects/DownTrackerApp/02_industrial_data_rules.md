# AGENT BEHAVIOR & SYSTEM PROMPT
Saha loglarını analiz ederken string araması (.Contains) yerine nesne eşitliği kullanacaksın. Arıza durumlarının kök neden alarmları, saniye veya süreden tamamen bağımsız olarak, sadece ve sadece o arızanın içinde gerçekleştiği iki TransactionEnd = True (üretim çevrimi) sınırları içerisinde aranır.

---

# 2. Endüstriyel Saha ve Algoritma Anayasası

## ⚙️ Veri Okuma ve Kararlı Parçalama Standartları (Parsing Engine)

- **Dosya Tipi:** Standart CSV. Ayırıcı karakter kesinlikle Virgül (`,`) olarak baz alınacaktır.
- **Veri Temizliği (Crucial):** Satır virgüle göre split edildikten sonra, elde edilen her string verinin başında ve sonunda bulunabilecek tüm çift tırnak (`"`) ve boşluk karakterleri kesin olarak temizlenecektir (`.Trim('"', ' ')`).
- **Zaman Hassasiyeti (TagDate):** Tarihler `yyyy-MM-dd HH:mm:ss` formatında tam saniye hassasiyetiyle `DateTime` nesnesine parse edilecektir. Tüm veri listesi kronolojik olarak (eskiden yeniye) saniye bazlı sıralanacaktır.
- **Nesne Tabanlı İstasyon/Robot Belirleme:** `TagName` string'inin içinde açıkça hangi alt grup kodu (Örn: `8F35`, `8Y50`, `9J39`, `8C70` vb.) geçiyorsa, log nesnesinin `StationName` property'sine doğrudan o değer atanacaktır. String içinde `-R1`, `.R2` gibi robot ekleri varsa bunlar temizlenip `RobotName` property'sine yazılacaktır. Arayüzde asla `"-"` görünmeyecektir.

## 🔒 Katı İş Mantığı ve İstasyon İzolasyonu (Quarantine)

- **Sinyal Sonu Tanımları:**
  - `.Outputs.Transactionend` (Sadece `True` olanlar geçerli bir üretim penceresi başlatır/kapatır).
  - `.Outputs.ST_DownCondition` (True = Arıza başlangıcı, False = Arıza bitişi).
  - `.Errors.Alarm.DiscreteAlarm` (True = Kök neden adayı alarm sinyali).
- **Ortak Çevrim / Ortak Referans Mantığı:** İstasyonun altındaki tüm robot assetlerinin (R1, R2, R3 vb.) kendilerine özel ayrı bir Transactionend sinyali yoktur. İstasyon ve o istasyona bağlı tüm robotlar, üretim zaman aralıklarını (pencerelerini) belirlemek için istasyonun kendi meşru ortak `.Outputs.Transactionend` sinyalini ortak referans alır. Bir robotun arıza durumu (ST_DownCondition) işlenirken, o robotun bağlı olduğu üst istasyonun üretim çevrim pencereleri baz alınacaktır.
- **Nesne Tabanlı Katı İzolasyon (Zero Cross-Contamination):** Filtreleme ve mantık motoru çalışırken string araması (`.Contains`) KESİNLİKLE YAPILMAYACAKTIR. Filtreleme doğrudan nesne eşitliği üzerinden yürütülecektir: `if (log.StationName == selectedStation)`. Bir istasyonun ekranına veya raporuna, ismi açıkça o istasyonla birebir eşleşmeyen HİÇBİR log veya duruş satırı zamanı ne olursa olsun SIZAMAZ, eklenemez, tamamen bloke edilir.
- **Katı Üretim Çevrimi Sınırı:** Bir istasyonda arıza (`ST_DownCondition = True`) işlenebilmesi için, o arızanın zaman damgasının kesinlikle o istasyonun kendi meşru iki `Transactionend = True` sinyali arasında gerçekleşmiş olması şarttır. Dosyada ilgili istasyona ait hiç `Transactionend` verisi yoksa, o istasyonun Üretim Çevrimleri ve Arıza listesi **TAMAMEN BOMBOŞ** kalmalıdır (Asla sahte veya varsayılan çevrim uydurulmayacaktır).
- **Arızanın Çevrimle Kesilmesi (Downtime Splitting):** Devam eden bir arıza varken araya yeni bir `Transactionend = True` girerse, arıza o noktada bölünür. İlk parça o çevrimdeki ilk alarmı kök neden alır. İkinci parça yeni çevrim zamanında başlar ve gerçek `Down=False` ile biter, kök nedenini yeni çevrimde arar (bulamazsa Boş Alarm olarak turuncu listelenir).
- **Ham Veri Serbestliği:** `Ham Veri` sekmesi bu filtrelerden tamamen muaf olup, CSV'deki tüm satırları saniyeleriyle birlikte ham olarak her koşulda listeler.

## 🐛 Çözülen Kritik Hatalar ve Bug Çözümleri

- **Ana İstasyon Asenkron Alarm ve Propagation Delay Hatası (Çözüldü):**
  - **Sorun:** Robot arızaya geçtiğinde (`9A30LH-R1...ST_DownCondition = True`), gerçek hata alarm kodunu (`DiscreteAlarm5`) ana istasyondan 1.4 saniye önce ateşlemektedir. Ana istasyon (`9A30LH.-`) duruş sinyalini aldığında tam o saniyeye baktığı için alarmı ıskalamakta ve ekrana **'Boş Arıza' (Boş Alarm)** basmaktadır.
  - **Çözüm (ParsingEngine.cs):** Arıza durumlarının kök neden alarmları, saniye veya süreden tamamen bağımsız olarak, sadece ve sadece o arızanın içinde gerçekleştiği iki TransactionEnd = True (üretim çevrimi) sınırları içerisinde aranır.

## 🧙‍♂️ Import Sihirbazı Haritalama Kuralları

- **Kaynak Kolon Haritalaması (0-tabanlı indeksler):**
  - **FULLTAGNAME:** Kaynak R Sütunu (İndex 17)
  - **DEVICENAME:** Kaynak B Sütunu (İndex 1)
  - **COMMENT:** Kaynak O Sütunu (İndex 14)
  - **ParentCategory:** Kaynak P Sütunu (İndex 15)
  - **Category:** Kaynak Q Sütunu (İndex 16)
  - **Code:** Excel'in dinamik olarak hesaplaması için satır bazlı olarak E sütununu referans alan formül şeklinde (`=IF(E2="Acil Stop (M)","FC-1000",...)`) `<f>` etiketleri ile hücreye yazılır.
- **Güvenlik ve Hata Önleme Standardı (Boundaries Checking):**
  - Olası eksik sütun durumlarında ve dizi sınır aşımı hatalarında (Out of Range) uygulamanın çökmesini engellemek için, her satırın okuma bloğunda mutlak suretle `parts.Length > 17` kontrolü işletilmelidir. Eksik indeks içeren satırlar atlanmalıdır.
- **Çıktı Dosyası Yapısı (Multi-Sheet):**
  - Dosya adı zaman damgalı formatta kaydedilir: `ImportSihirbazı_yyyyMMdd_HHmmss.xlsx`.
  - Harici paket bağımlılığını (ClosedXML, EPPlus) ortadan kaldırmak için, çıktı dosyası saf C# OpenXML Zip yapısıyla (`ZipArchive`) gerçek ve hatasız bir Excel dosyası olarak üretilir.
  - Dosyada iki sayfa yer alır:
    1. **ImportP360:** Ana veri sayfası (Sheet 1). F sütununda dinamik formüller yer alır.
    2. **CodeFormula:** Haritalama formülünün tamamının düz metin (inlineStr) olarak A3 hücresinde mühürlendiği rehber sayfası (Sheet 2). Calibri fontunda olup ilk sütun genişliği `160` olarak ayarlanmıştır. A1 hücresi koyu mavi bold arka plan dolgusu ile başlık içerir.
- **Örnek Şablon Üretici (GenerateInputTemplate):**
  - Dosya adı `"P360_Beklenen_Input_Sablonu.xlsx"` olarak varsayılanlandırılmıştır.
  - Harici paket bağımlılığı olmaksızın, saf OpenXML Zip formatında 18 sütun başlığını (`CHANNEL`'dan `FULLTAG`'e kadar) ve 1 adet gerçekçi örnek veri satırını mühürleyecek şekilde otonom bir yapıya sahiptir.
  - Olası I/O hatalarına karşı `try-catch-finally` güvenlik sarmalı ile kurgulanmıştır.
- **Sütun Başlık Renk Matrisi (11 Farklı Dolgu Rengi):**
  - Girdi önizleme gridi (`dgvInputPreview`), çıktı önizleme gridi (`dgvOutputPreview`) ve üretilen şablon ile nihai Excel çıktısı sayfalarında başlık hücreleri aşağıdaki renk matrisine göre boyanır ve bold olarak Calibri 12pt formatında siyah renkle basılır:
    1. `CHANNEL NAME` & `DEVICE NAME` -> Koyu Sarı / Turuncu (`#FFC000` / `RGB 255, 192, 0`)
    2. `LINE NAME (LVL1/LVL2/LVL3)` -> Yumuşak Yeşil (`#92D050` / `RGB 146, 208, 80`)
    3. `STATION NAME` -> Canlı Mavi (`#00B0F0` / `RGB 0, 176, 240`)
    4. `Res` & `Code` -> Standart Gri (`#BFBFBF` / `RGB 191, 191, 191`)
    5. `Tag Name` & `PLC Adress` -> Saf Sarı (`#FFFF00` / `RGB 255, 255, 0`)
    6. `Data Type` -> Soluk Kırmızı / Pembe (`#D99694` / `RGB 217, 150, 148`)
    7. `A/D` & `RO//R/W` -> Açık Gri (`#D9D9D9` / `RGB 217, 217, 217`)
    8. `SCAN RATE` -> Toz Mavi (`#92CDDC` / `RGB 146, 205, 220`)
    9. `COMMENT` -> Şeftali / Krem (`#FCE4D6` / `RGB 252, 228, 214`)
    10. `Category Name` & `SubCategory Name` -> Soluk Yeşil (`#EAF1DD` / `RGB 234, 241, 221`)
    11. `FULL TAG NAME` -> Açık Çelik Mavi (`#B7DEE8` / `RGB 183, 222, 232`)
- **Calibri Yazı Tipi ve Boyutu Standartları:**
  - **Başlık Satırı:** `Calibri, 12pt, Bold` (Siyah metin, GDI+ ile özel boyanır, Excel'de `<b/>` etiketli fontId ve styles.xml ile mühürlenir).
  - **Veri Hücreleri:** `Calibri, 11pt, Regular` (Açık Gri `#DCDCDC` metin, koyu tema `#121212` arka planlıdır).
- **Otonom Dosya Akışı:**
  - Herhangi bir dönüştürme butonuna ihtiyaç kalmaksızın, kullanıcı dosyayı sürükleyip bıraktığında veya alana tıklayarak seçtiğinde `ProcessAndSaveFileAsync` asenkron süreci tetiklenir.
  - İşlemler bittiğinde `SaveFileDialog` otomatik olarak kaydetme ekranını kullanıcının önüne getirir ve kaydetme onaylandığında Excel otonom olarak kaydedilir.


