using RadarTendencias.Worker;

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
    client.DefaultRequestHeaders.Add("User-Agent", "windows:radartendencias:v1.0 (by /u/erick)");
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
