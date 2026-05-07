using SolunumProjesi.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<SolunumProjesi.Services.AtakKaydiCsvService>();
builder.Services.AddSingleton<SolunumProjesi.Services.MLModelService>();
builder.Services.AddHttpClient<SolunumProjesi.Services.AISimulationService>();

var app = builder.Build();

// ML Mock.csv ile modeli uygulama başlarken eğit
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
