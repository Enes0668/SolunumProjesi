namespace SolunumProjesi.Models;

// Bir hastanın solunum atağına ait tüm verileri ve analiz sonuçlarını tutan model sınıfı
public class AtakKaydi
{
    // CSV'den gelen benzersiz kayıt numarası
    public int Id { get; set; }

    // Hastanın peak flow ölçüm değeri (L/dk); yüksek değer daha iyi nefes kapasitesini gösterir
    public int PeakFlowValue { get; set; }

    // Öksürük sıklığı: Az / Orta / Cok / Surekli
    public string CoughFrequency { get; set; } = "";

    // Nefes darlığı şiddet skoru (0-10 arası)
    public int ShortnessOfBreath { get; set; }

    // Hastanın gece uyku sırasında uyanıp uyanmadığı
    public bool NightWaking { get; set; }

    // CSV sütunlarından türetilen okunabilir semptom özeti
    public string Semptomlar { get; set; } = "";

    // Göğüs hastalıkları uzmanının manuel olarak atadığı etiket (Hafif / Orta / Siddetli)
    public string UzmanEtiketi { get; set; } = "";

    // Gemini API'nin tahmin ettiği etiket; analiz yapılmadan önce boş kalır
    public string AIEtiketi { get; set; } = "";

    // Gemini API'nin tahminine ait klinik açıklama metni
    public string AIGerekce { get; set; } = "";

    // ML.NET modelinin tahmin ettiği etiket; CSV yüklenince otomatik doldurulur
    public string MLEtiketi { get; set; } = "";
}
