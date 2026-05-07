namespace SolunumProjesi.Models;

public class AtakKaydi
{
    public int Id { get; set; }
    public int PeakFlowValue { get; set; }
    public string CoughFrequency { get; set; } = "";
    public int ShortnessOfBreath { get; set; }
    public bool NightWaking { get; set; }
    public string Semptomlar { get; set; } = "";
    public string UzmanEtiketi { get; set; } = "";
    public string AIEtiketi { get; set; } = "";
    public string AIGerekce { get; set; } = "";
    public string MLEtiketi { get; set; } = "";
}
