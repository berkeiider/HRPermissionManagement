HR Permission Management 
(İnsan Kaynakları İzin Yönetim Sistemi)

 Proje Amacı
Bu proje, geleneksel ve kağıt tabanlı şirket içi izin yönetim süreçlerini modernize ederek dijital ortama taşımayı amaçlamaktadır. Temel hedefler şunlardır:
•	Verimlilik: İzin talep ve onay mekanizmalarını hızlandırarak insan kaynakları operasyonlarındaki iş yükünü hafifletmek.
•	Şeffaflık: Çalışanların izin bakiyelerini ve talep durumlarını anlık olarak takip edebilmesini sağlamak.
•	Merkezi Yönetim: Yöneticilere, ekiplerinin izin durumlarını tek bir panel üzerinden yönetme ve raporlama imkanı sunmak.
•	Sürdürülebilirlik: Kağıt israfını önleyerek çevre dostu bir iş akışı oluşturmak.

Uygulama, hiyerarşik bir yapıda üç temel kullanıcı grubuna hitap etmektedir:
1.	Şirket Çalışanları (Kullanıcılar): Sisteme giriş yaparak izin talebi oluşturan, geçmiş izinlerini sorgulayan ve kalan izin bakiyelerini görüntüleyen personel.
2.	Departman Yöneticileri: Kendi ekiplerindeki personelin izin taleplerini görüntüleme, değerlendirme (onaylama/reddetme) yetkisine sahip orta düzey yöneticiler.
3.	İnsan Kaynakları / Admin: Sistemin genel kontrolünü sağlayan, şirket politikalarına göre izin türlerini tanımlayan, yeni çalışan/departman ekleyen ve tüm süreci denetleyen üst düzey yetkililer.

 Kullanım Amacı
 
Proje, kurumsal bir firmadaki izin sürecini simüle eden aşağıdaki iş akışını uygular:

1. Güvenli Giriş ve Kimlik Doğrulama
•	Kullanıcılar sisteme e-posta ve şifreleri ile giriş yapar.
•	Veri güvenliği kapsamında tüm şifreler veritabanında SHA-256 algoritmasıyla hashlenmiş (şifrelenmiş) olarak saklanır.



2. Talep Oluşturma ve Validasyonlar
Çalışan, izin türünü (Yıllık, Mazeret vb.) ve tarih aralığını seçer. Sistem bu aşamada şu kontrolleri otomatik yapar:
•	Tarih Tutarlılığı: Bitiş tarihi, başlangıç tarihinden önce olamaz.
•	Çakışma Kontrolü: Seçilen tarihlerde çalışanın halihazırda onaylanmış veya bekleyen başka bir izni olup olmadığı denetlenir.
•	Bakiye Kontrolü: Talep edilen gün sayısı, çalışanın mevcut izin bakiyesinden fazla olamaz.
3. Yönetim ve Onay Süreci
•	Departman Bazlı Filtreleme: Yöneticiler sadece kendi departmanlarına bağlı personelin taleplerini görür.
•	Admin Yetkisi: Admin kullanıcıları, departman bağımsız tüm talepleri görüntüleyebilir ve müdahale edebilir.
4. Takip ve Güncelleme
•	Yönetici onayı verildiğinde, ilgili gün sayısı çalışanın izin bakiyesinden otomatik olarak düşülür.
•	Çalışanlar, "İzinlerim" paneli üzerinden taleplerinin durumunu (Bekliyor, Onaylandı, Reddedildi) renk kodları ile anlık takip edebilir.

Kullanılan Teknolojiler
Proje geliştirme sürecinde modern yazılım mimarileri ve aşağıdaki teknoloji yığını kullanılmıştır:
| Alan | Teknoloji / Kütüphane | Açıklama |
| :--- | :--- | :--- |
| **Programlama Dili** | C# | Backend geliştirme dili. |
| **Framework** | ASP.NET Core MVC | Model-View-Controller mimarisi üzerine kurulu web uygulama. |
| **Veritabanı** | SQL Server Express | İlişkisel veritabanı yönetim sistemi. |
| **ORM** | Entity Framework Core | Veritabanı işlemleri için Code-First yaklaşımı. |
| **Arayüz (UI)** | HTML5, Tailwind CSS | Responsive (mobil uyumlu) ve modern arayüz tasarımı. |
| **Frontend** | HTML5 / CSS3 / JS | İstemci tarafı etkileşimleri ve validasyonları. |



Demo Videosu:

Projenin canlı kullanım senaryosunu, arayüzlerini ve özelliklerini detaylı incelemek için aşağıdaki bağlantıya tıklayabilirsiniz:

https://youtu.be/1dv_CZbiIeE
