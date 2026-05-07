using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using SolunumProjesi.Models;

namespace SolunumProjesi.Services;

public class AtakKaydiCsvService
{
    public List<AtakKaydi>? GuncelVeriler { get; private set; }

    public List<AtakKaydi> ReadVeriler(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV dosyası bulunamadı: {filePath}");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<AtakKaydiMap>();

        var records = csv.GetRecords<AtakKaydi>().ToList();
        GuncelVeriler = records;
        return records;
    }
}

public sealed class AtakKaydiMap : ClassMap<AtakKaydi>
{
    public AtakKaydiMap()
    {
        Map(m => m.Id).Name("id");
        Map(m => m.PeakFlowValue).Name("peak_flow_value");
        Map(m => m.CoughFrequency).Name("cough_frequency");
        Map(m => m.ShortnessOfBreath).Name("shortness_of_breath");
        Map(m => m.NightWaking).Name("night_waking");

        Map(m => m.Semptomlar).Convert(args =>
        {
            var row = args.Row;
            var cough = row.GetField("cough_frequency");
            var shortness = row.GetField("shortness_of_breath");
            var night = row.GetField("night_waking");
            return $"Öksürük: {cough}, Nefes Darlığı: {shortness}, Gece Uyanması: {night}";
        });

        Map(m => m.UzmanEtiketi).Name("expert_label");

        Map(m => m.AIEtiketi).Ignore();
        Map(m => m.AIGerekce).Ignore();
        Map(m => m.MLEtiketi).Ignore();
    }
}
