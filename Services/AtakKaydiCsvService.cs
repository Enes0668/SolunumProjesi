using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using SolunumProjesi.Models;

namespace SolunumProjesi.Services;

// CSV dosyasını okuyup AtakKaydi listesine dönüştüren servis; uygulama genelinde tek örnek (Singleton) olarak çalışır
public class AtakKaydiCsvService
{
    // Son yüklenen veri seti; sayfalar arası paylaşım için burada saklanır
    public List<AtakKaydi>? GuncelVeriler { get; private set; }

    // Verilen dosya yolundaki CSV'yi okur, listeye çevirir ve GuncelVeriler'e kaydeder
    public List<AtakKaydi> ReadVeriler(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV dosyası bulunamadı: {filePath}");
        }

        // Başlık satırı var; eksik alan veya hatalı veri satırlarını sessizce atla
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        // Sütun adı → model alanı eşlemesini özel map sınıfıyla yap
        csv.Context.RegisterClassMap<AtakKaydiMap>();

        var records = csv.GetRecords<AtakKaydi>().ToList();
        GuncelVeriler = records;
        return records;
    }
}

// CSV sütun adlarını AtakKaydi model alanlarıyla eşleştiren yapılandırma sınıfı
public sealed class AtakKaydiMap : ClassMap<AtakKaydi>
{
    public AtakKaydiMap()
    {
        Map(m => m.Id).Name("id");
        Map(m => m.PeakFlowValue).Name("peak_flow_value");
        Map(m => m.CoughFrequency).Name("cough_frequency");
        Map(m => m.ShortnessOfBreath).Name("shortness_of_breath");
        Map(m => m.NightWaking).Name("night_waking");

        // Semptomlar alanı CSV'de doğrudan yok; birden fazla sütun birleştirilerek oluşturulur
        Map(m => m.Semptomlar).Convert(args =>
        {
            var row = args.Row;
            var cough = row.GetField("cough_frequency");
            var shortness = row.GetField("shortness_of_breath");
            var night = row.GetField("night_waking");
            return $"Öksürük: {cough}, Nefes Darlığı: {shortness}, Gece Uyanması: {night}";
        });

        Map(m => m.UzmanEtiketi).Name("expert_label");

        // Bu alanlar CSV'de bulunmaz; uygulama tarafından doldurulur, eşlemeden çıkar
        Map(m => m.AIEtiketi).Ignore();
        Map(m => m.AIGerekce).Ignore();
        Map(m => m.MLEtiketi).Ignore();
    }
}
