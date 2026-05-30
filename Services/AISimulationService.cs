using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SolunumProjesi.Models;

namespace SolunumProjesi.Services;

// Google Gemini API'yi kullanarak hasta verilerini analiz eden ve klinik etiket üreten servis
public class AISimulationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    // Gemini 2.5 Flash modeline istek gönderilecek endpoint
    private const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    public AISimulationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        // API anahtarı appsettings.Development.json'dan okunur; eksikse uygulama başlamaz
        _apiKey = configuration["GeminiApiKey"]
            ?? throw new InvalidOperationException("GeminiApiKey, appsettings.Development.json içinde bulunamadı.");
    }

    // Seçilen hasta kaydını Gemini'ye gönderir; etiket ve klinik gerekçe döner
    public async Task<(string Etiket, string Gerekce)> AnalizYapAsync(AtakKaydi kayit)
    {
        try
        {
            var prompt = $@"Sen bir göğüs hastalıkları uzmanı yapay zeka asistanısın.
Hastanın verileri:
- Peak Flow (L/dk): {kayit.PeakFlowValue}
- Semptomlar: {kayit.Semptomlar}
- Hastayı önceden değerlendiren uzmanın verdiği etiket: {kayit.UzmanEtiketi}

Görevlerin:
1. Hastanın durumunu klinik olarak değerlendir ve durumuna uygun olarak 'Hafif', 'Orta' veya 'Siddetli' etiketlerinden SADECE BİRİNİ dön.
2. Bu etiketi neden verdiğini, hastanın peak flow değerini ve semptomlarını göz önünde bulundurarak açıkla. Açıklamanda ayrıca uzmanın '{kayit.UzmanEtiketi}' kararına katılıp katılmadığını ve nedenini de belirt. Uzmanın kararıyla senin kararın arasında karşılaştırma yap.

Lütfen cevabını SADECE aşağıdaki JSON formatında, hiçbir markdown etiketi kullanmadan, düz metin gibi dön:
{{
  ""Etiket"": ""Hafif"",
  ""Gerekce"": ""Klinik açıklama ve uzmanla görüş karşılaştırması buraya...""
}}";

            // Gemini'ye gönderilecek istek gövdesi; responseMimeType ile JSON çıktısı zorunlu kılınır
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            // API anahtarı URL'e query string olarak eklenir
            var url = $"{GeminiEndpoint}?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var hataGovdesi = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Gemini API HTTP {(int)response.StatusCode}: {hataGovdesi}");
                return ("API Hatası", $"Gemini {(int)response.StatusCode} hatası: {hataGovdesi}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(responseJson);

            // Gemini yanıtı candidates[0].content.parts[0].text yolunda bulunur
            var root = jsonDocument.RootElement;
            var responseText = root
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return ("Hata", "API boş bir yanıt döndürdü.");
            }

            // Yanıt metnindeki olası markdown kod bloğu işaretlerini temizleyip JSON olarak ayrıştır
            var aiResult = JsonDocument.Parse(responseText.Trim('`', '\n', ' ')).RootElement;

            var etiket = aiResult.TryGetProperty("Etiket", out var etiketElement) ? etiketElement.GetString() : "Bilinmiyor";
            var gerekce = aiResult.TryGetProperty("Gerekce", out var gerekceElement) ? gerekceElement.GetString() : "Gerekçe okunamadı.";

            return (etiket, gerekce);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gemini API Error: {ex.Message}");
            return ("API Hatası", $"Yapay zeka analizini gerçekleştirirken bir hata oluştu: {ex.Message}");
        }
    }
}
