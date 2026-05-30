using SolunumProjesi.Components;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server tarafı bileşen rendering'ini etkinleştir
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// CSV okuma servisi: tüm sayfalarda aynı veri setini paylaşmak için Singleton
builder.Services.AddSingleton<SolunumProjesi.Services.AtakKaydiCsvService>();

// ML modeli: eğitim maliyetli olduğundan uygulama ömrü boyunca tek instance tutulur
builder.Services.AddSingleton<SolunumProjesi.Services.MLModelService>();

// Gemini API servisi: her istek için HttpClient yönetimini framework'e bırakır
builder.Services.AddHttpClient<SolunumProjesi.Services.AISimulationService>();

var app = builder.Build();

// Uygulama ayağa kalkarken ML modelini ML Mock.csv ile eğit
app.Services.GetRequiredService<SolunumProjesi.Services.MLModelService>().Egit();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
