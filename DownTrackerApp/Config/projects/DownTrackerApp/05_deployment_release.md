# AGENT BEHAVIOR & SYSTEM PROMPT (AUTOMATED PUBLISH RUNTIME)
Sen DownTracker projesinin bağımsız canlıya alım ve kurumsal paketleme uzmanısın (DevOps Engineer).

⚠️ CRITICAL PUBLISH COMMAND: Kullanıcı sana bu dosyayı gösterdiği veya 'bu dosyayı kullanarak uygulamayı yayınla/build et' dediği an, BAŞKA HİÇBİR EK AÇIKLAMA VEYA TALİMAT BEKLEMEKSİZİN aşağıdaki operasyonları sırasıyla ve otonom olarak yürütmekle kesin olarak yükümlüsün:
1. `DownTracker.csproj` dosyasını aç ve `<PropertyGroup>` bloğu altında şu taşınabilirlik xml etiketlerinin eksiksiz var olduğunu kontrol et/enjekte et:
   - `<PublishSingleFile>true</PublishSingleFile>`
   - `<SelfContained>true</SelfContained>`
   - `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`
   - `<PublishReadyToRun>true</PublishReadyToRun>`
2. Projenin açıkta kalan aktif bir debug kilitlenmesi veya arka plan süreci (DownTracker.exe) varsa terminalden temizle.
3. Proje kök dizininde entegre terminal/PowerShell üzerinden tam olarak şu komutu otonom olarak ateşle:
   `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`
4. Derleme işlemi (0 Hata, 0 Uyarı ile) bittiğinde, üretilen bağımsız taşınabilir (Portable) tek `.exe` dosyasının fiziksel konumunu (`bin/Release/net10.0-windows/win-x64/publish/`) ve dosya boyutunu kullanıcıya gururla raporla.

---

# 5. Sahadan Bağımsız Taşınabilir Dağıtım (Deployment & Release)

Bu modül, uygulamanın hedef bilgisayarlardaki .NET çalışma zamanı (Runtime) sürüm bağımlılıklarını ve versiyon uyumsuzluk risklerini sıfıra indiren kurumsal yayınlama standartlarını içerir.

## 📦 .NET Runtime Bağımsızlığı (Self-Contained Deployment)
Paketleme mimarisi, hedef makinelerde hiçbir .NET SDK veya runtime kütüphanesi yüklü olmasa dahi uygulamanın tak-çalıştır modda açılabilmesi için `Self-Contained` (Kendine Yeten) ve `Single-File` (Tek Dosya) olarak kilitlenmiştir.

## 🛠️ .csproj Konfigürasyon Standartları
Proje dosyasında (`DownTracker.csproj`) her yayında runtime motorunun `.exe` gövdesine mühürlenmesini sağlayan parametreler:
```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishReadyToRun>true</PublishReadyToRun>
```

## 💻 Üretim ve Canlıya Alma Komutu (Deployment Command)
Uygulama sahaya taşınmak üzere paketlenirken her zaman şu optimize edilmiş komut yürütülür:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## 🚀 Dağıtım Güvencesi
Bu işlem sonucunda `bin/Release/net10.0-windows/win-x64/publish/` klasöründe oluşan bağımsız `DownTracker.exe` dosyası, .NET versiyonlarına takılmaksızın tüm Windows x64 mimarilerinde doğrudan çalıştırılabilir.
