# StudyMate AI - Proje Dokümantasyonu

StudyMate AI, öğrencilerin ders çalışma, planlama, sınavlara hazırlık ve doküman analizi süreçlerini yerel yapay zeka (Ollama) gücüyle tek bir çatı altında toplayan modern bir .NET 8 (WPF) Masaüstü uygulamasıdır. 

---

## 🏗 Proje Mimarisi: MVVM (Model-View-ViewModel)
Proje, klasik WPF pratiklerine uyumlu olarak **MVVM mimarisi** ile inşa edilmiştir. Arayüz ve iş kuralları (backend logic) birbirinden tamamen ayrı tutulmuştur.

1. **Model:** Veri yapılarını (`Class`) temsil eder. (Örn: `StudyTask`, `Course`, `DailySchedule`, `ExamResult` vb.)
2. **View:** Kullanıcı Arayüzü (UI) `XAML` kodları. Tasarım kuralları ve element konumları burada belirlenir. İş kodlarını içermemesi hedeflenir.
3. **ViewModel:** Model verilerini alır, işler (mantıksal işlemleri sağlar) ve Data Binding mekanizması aracılığıyla `View` (Arayüz) tarafına gönderir. Tıklama aksiyonları `ICommand` yapısı ile yönetilir.

---

## 🛠 Kullanılan Teknolojiler ve Kütüphaneler
- **.NET 8.0 & C#:** Temel dil ve çalışma zamanı.
- **WPF (Windows Presentation Foundation):** PC masaüstü masaüstü arayüz çerçevesi.
- **Material Design In XAML Toolkit:** Uygulama içerisinde kullanılan hazır ikonlar (PackIcon) ve bazı gelişmiş araçlar.
- **Microfort.Web.WebView2:** PDF görüntüleyici (`DocumentsPage`) ve dahili YouTube oynatıcı (`VideoPlayerPage`) alanlarında render motoru olarak kullanılır.
- **Ollama**: Tamamen bilgisayarda (yerelde) veya belirlediğiniz yerel sunucuda çalışan açık kaynaklı LLM'ler (qwen2.5, llama3 vb.) aracılığıyla gizli ve çevrimdışı/sınırsız yapay zeka sunar.
- **SQLite / Entity Framework Core (veya Local Json):** Yerel veritabanı iletişimi için.

---

## 🎨 Tasarım Dili ve "Dark Theme" Prensipleri
StudyMate AI'ın tasarımı baştan aşağı yenilenmiş ve göz yormayan, son derece uyumlu ve katmanlı bir **Flat Dark Mode** konseptine oturtulmuştur. Eski (varsayılan) Material tasarım gölgeli beyaz kartlarından feragat edilip şık, yuvarlatılmış paneller inşa edilmiştir. 

- **App.xaml (Uygulama Kalbi):** Tüm renk tanımlamaları ve `DynamicResource` fırçaları (brushes) burada tanımlanmıştır. Buton tipleri, TextBox ve ScrollBar gibi temel UI kuralları global olarak buraya gömülmüştür. 
- **Yassı `Border` Tasarımı:** Gölge kullanan Card temaları, `{DynamicResource BgCardBrush}` (örneğin #1E293B) renklere ve 6-8px `CornerRadius`'a çevrilmiştir.
- **Sıfır Beyaz Buton:** Tasarım boyunca tüm butonların (`PrimaryButton`, `NeonGlowButton`, `SecondaryButton`, `DangerButton`) tamamen ana temaya entegre edilmesi sağlanmıştır.

---

## 📁 Proje Klasör Yapısı (Dizilimi)

Projenizi açtığınızda karşınızda modern bir uygulama kalıbı vardır.

```text
StudyMateAI/
│
├── Models/              # Veritabanı ve sınıf tabloları. (User, Video, Document, Plan vs.)
├── ViewModels/          # Sayfaların iş (business) kuralları. Buton tıklamaları, API istekleri burada olur.
├── Views/               # .xaml tasarım dosyaları ve C# Code-Behind (.xaml.cs) dosyaları.
│   ├── ChatPage.xaml           # AI Serbest Sohbet asistanı.
│   ├── CoursesPage.xaml        # Ortalama/vize/final ve kayıtlı ders takibi alanı.
│   ├── DashboardPage.xaml      # Giriş (Ana) özet ekranı.
│   ├── DocumentsPage.xaml      # PDF'leri gösterip AI ile anında özet/plan hazırlama ekranı.
│   ├── ExamPage.xaml           # Videolardan veya belgelerden test/deneme çıkaran sınav simülatörü.
│   ├── StudyPlannerPage.xaml   # Otomatik / Manuel takvim tabanlı iş atama alanı.
│   ├── VideoPlayerPage.xaml    # YouTube veya yerel medyayı izleyip, yan ekranda AI'a transkript iletilmesi.
│   └── SettingsPage.xaml       # Ollama ayarları (model seçimi) ve kişiselleştirme.
│
├── Services/            # Arka plan entegrasyon dosyaları. 
│   ├── OllamaService.cs        # Yerel Yapay zekaya HTTP REST üzerinden veri yollayan merkez.
│   └── AudioVideoTrancriber.cs # Mevcutsa videodan yazı çıkarma servisleri.
│
├── Helpers/             # Data converter (BooleanToVisibilityConverter) vb.
├── App.xaml             # Global Temalar
└── MainWindow.xaml      # Ana Pencere İskeleti (Menü / Üst Bar buradır).
```

---

## 🧠 Merkezi Mantıklar (Genel Yetenekler)

### 1. Ollama Entegrasyonu
`OllamaService` uygulamada sürekli aktiftir. `SettingsPage` üzerinden kullanıcı döküman için farklı model, sohbet için farklı model (Örn; hafif vizyon-metin modelleri `qwen2.5-vl`) tanımlayabilir.
Uygulama, kullanıcının komutlarından sonra sisteme HTTP Request atıp arka planda dönen (Stream) cevapları `Task (Async/Await)` ile ViewModel'a günceller.

### 2. Sınav Simülatörü ve Test Doğrulama (`ExamPage.xaml`)
Kullanıcı PDF veya Video modunu seçip **"Zor Sınav Başlat"** dediğinde:
1. Kaynak içerikten metin çıkarılır (Parse edilir).
2. JSON talimatıylla arka planda Ollama'dan "10 Çoktan Seçmeli / 3 Klasik soru ver" kodu yollanır.
3. Ekrana sorular gelir. **Klasik Değerlendirme Modu:** Kullanıcı cevabı uzunca yazar, LLM bu cevabı **"Puan(0-100) ve Nedenini"** içerecek şekilde analiz edip not verir. Süreç tamamen otomatik ilerler.

### 3. Çalışma Planlayıcısı Sürükle-Bırak (`StudyPlannerPage.xaml`)
Böl-ve-yönet stiline uygun tasarlanmıştır. AI, hedef konuları alt "Görevler (Task)" parçalarına ayırır.
Kullanıcılar WPF'nin (DataObject ve DragDrop) özellikleri sayesinde `Task` elementini tutup Pazartesi, Salı, Çarşamba panellerine (DailySchedule) bırakarak günlere görev dağıtabilir. Ekrandaki sayaç o seans için ilerlemeyi barındırır.

### 4. WebView2 Döküman ve Medya Yönetimi
Hem video sayfası hem döküman sayfası `Microsoft.Web.WebView2` kontrolünden gücünü alır. Böylece YouTube JavaScript api'si sorunsuz çalıştırılırken, ek bir PDF motoru yüklemeye gerek kalmadan EDGE altyapısının sağladığı güçlü web pdf editör motoruyla anında performans elde edilir. 

---

## 🚀 Projenin Mevcut Durumu ve Nasıl Çalıştırılır?
Masaüstü uygulamamız, hem MVVM hem modern XAML altyapısına sahiptir. Uygulama XAML tarafında `Layout`, `StackPanel` ve `Grid` mimarisini kullanarak ekran genişliklerine tam reaktif destek verir.

**Çalıştırmak için Terminalde (Projeye konumlu dizinde):**
1. Paketlerin derlenmesi: `dotnet build`
2. Uygulamayı başlatma: `dotnet run`
3. Ollama bağlantısının kopmaması adına arka planda Ollama yerel sunucusunun donanımda uyandığından (açık olduğundan) emin olmalısınız.

> *Bu doküman, geliştirme sırasında hem yeni bir yazılımcının hem de sizin projenizin kapsamını anlamasına olanak tanımak amacıyla güncel "Dark Theme" ve yetenek kurallarına uygun olarak Antigravity tarafından derlenmiştir.*
