using RadarTendencias.Worker;
using RadarTendencias.Worker.Features.Servicos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient("JikanClient", client =>
{
    client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
});

builder.Services.AddHttpClient("TmdbClient", client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["TmdbApiKey"]}");
});

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8080");
});

builder.Services.AddHttpClient("NlpClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["NlpBaseUrl"] ?? "http://localhost:5000");
});

builder.Services.AddHttpClient("RedditClient", client =>
{
    client.BaseAddress = new Uri("https://www.reddit.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 RadarTendenciasBot/1.0");
});

builder.Services.AddSingleton<IAnaliseService, AnaliseService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
