using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Globalization;
using SolunumProjesi.Models;

namespace SolunumProjesi.Services;

internal class AtakVeri
{
    public float PeakFlow { get; set; }
    public string OksurukSikligi { get; set; } = "";
    public float NefesDarligi { get; set; }
    public float GeceUyanmasi { get; set; }
    [ColumnName("Label")]
    public string Etiket { get; set; } = "";
}

internal class AtakTahmini
{
    [ColumnName("PredictedLabel")]
    public string TahminEtiket { get; set; } = "";
}

internal sealed class MockCsvSatir
{
    public float peak_flow_value { get; set; }
    public string cough_frequency { get; set; } = "";
    public float shortness_of_breath { get; set; }
    public string night_waking { get; set; } = "";
    public string expert_label { get; set; } = "";
}

// ML.NET kullanarak solunum atağı şiddetini sınıflandıran servis
public class MLModelService
{
    private readonly MLContext _mlContext;

    // Modelin eğitileceği hazır veri dosyasının yolu
    private readonly string _mockCsvPath;

    // Eğitim tamamlanınca oluşturulan tahmin motoru; CSV yüklendikçe kullanılır
    private PredictionEngine<AtakVeri, AtakTahmini>? _tahminMotoru;

    public bool ModelEgitildi { get; private set; } = false;
    public int EgitimKayitSayisi { get; private set; } = 0;
    public string? HataMesaji { get; private set; }

    public MLModelService(IWebHostEnvironment env)
    {
        // Seed sabitlenmesi: her çalıştırmada aynı sonuçları üretmek için
        _mlContext = new MLContext(seed: 42);
        _mockCsvPath = Path.Combine(env.ContentRootPath, "ML Mock.csv");
    }

    // Modeli ML Mock.csv verisiyle eğitir; uygulama başladığında Program.cs tarafından çağrılır
    public void Egit()
    {
        ModelEgitildi = false;
        HataMesaji = null;

        if (!File.Exists(_mockCsvPath))
        {
            HataMesaji = $"ML Mock.csv bulunamadı: {_mockCsvPath}";
            Console.WriteLine(HataMesaji);
            return;
        }

        try
        {
            var egitimVerisi = OkuMockCsv();
            if (egitimVerisi.Count < 5)
            {
                HataMesaji = "ML Mock.csv'de yeterli kayıt yok (minimum 5).";
                return;
            }

            EgitimKayitSayisi = egitimVerisi.Count;

            var dataView = _mlContext.Data.LoadFromEnumerable(egitimVerisi);

            // Pipeline: etiket → sayısal anahtar, kategorik öksürük → one-hot, özellikler birleştir, SDCA ile sınıflandır
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    "OksurukSikligiEncoded", nameof(AtakVeri.OksurukSikligi)))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    nameof(AtakVeri.PeakFlow),
                    "OksurukSikligiEncoded",
                    nameof(AtakVeri.NefesDarligi),
                    nameof(AtakVeri.GeceUyanmasi)))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label", featureColumnName: "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var model = pipeline.Fit(dataView);

            // Eğitilmiş modelden tek kayıt tahmin edebilen hafif bir motor oluştur
            _tahminMotoru = _mlContext.Model.CreatePredictionEngine<AtakVeri, AtakTahmini>(model);
            ModelEgitildi = true;

            Console.WriteLine($"ML modeli eğitildi. Eğitim kayıt sayısı: {EgitimKayitSayisi}");
        }
        catch (Exception ex)
        {
            HataMesaji = $"Model eğitimi hatası: {ex.Message}";
            Console.WriteLine(HataMesaji);
        }
    }

    // Yüklenen hasta listesinin her kaydı için MLEtiketi alanını doldurur
    public void TahminEt(List<AtakKaydi> veriler)
    {
        if (!ModelEgitildi || _tahminMotoru == null || veriler == null) return;

        foreach (var kayit in veriler)
        {
            // AtakKaydi → AtakVeri dönüşümü; bool gece uyanması 0/1 float'a çevrilir
            var veri = new AtakVeri
            {
                PeakFlow = kayit.PeakFlowValue,
                OksurukSikligi = kayit.CoughFrequency ?? "",
                NefesDarligi = kayit.ShortnessOfBreath,
                GeceUyanmasi = kayit.NightWaking ? 1f : 0f,
                Etiket = ""
            };
            kayit.MLEtiketi = _tahminMotoru.Predict(veri).TahminEtiket ?? "Bilinmiyor";
        }
    }

    // ML Mock.csv'yi okur ve eğitim için AtakVeri listesine dönüştürür
    private List<AtakVeri> OkuMockCsv()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(_mockCsvPath);
        using var csv = new CsvReader(reader, config);

        return csv.GetRecords<MockCsvSatir>()
            .Select(r => new AtakVeri
            {
                PeakFlow = r.peak_flow_value,
                OksurukSikligi = r.cough_frequency ?? "",
                NefesDarligi = r.shortness_of_breath,
                // CSV'de "true"/"false" string olarak geliyor; float'a çevir
                GeceUyanmasi = r.night_waking?.ToLower() == "true" ? 1f : 0f,
                Etiket = r.expert_label ?? ""
            })
            .ToList();
    }
}
