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

public class MLModelService
{
    private readonly MLContext _mlContext;
    private readonly string _mockCsvPath;
    private PredictionEngine<AtakVeri, AtakTahmini>? _tahminMotoru;

    public bool ModelEgitildi { get; private set; } = false;
    public int EgitimKayitSayisi { get; private set; } = 0;
    public string? HataMesaji { get; private set; }

    public MLModelService(IWebHostEnvironment env)
    {
        _mlContext = new MLContext(seed: 42);
        _mockCsvPath = Path.Combine(env.ContentRootPath, "ML Mock.csv");
    }

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

    public void TahminEt(List<AtakKaydi> veriler)
    {
        if (!ModelEgitildi || _tahminMotoru == null || veriler == null) return;

        foreach (var kayit in veriler)
        {
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
                GeceUyanmasi = r.night_waking?.ToLower() == "true" ? 1f : 0f,
                Etiket = r.expert_label ?? ""
            })
            .ToList();
    }
}
