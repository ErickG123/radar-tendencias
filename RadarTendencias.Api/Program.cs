using DbUp;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Reflection;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RadarTendencias.Api.Features.DTOs;
using RadarTendencias.Api.Features.Franquias;
using RadarTendencias.Api.Features.Monitoramento;
using RadarTendencias.Api.Features.Favoritos;
using RadarTendencias.Api.Features.Notificacoes;
using RadarTendencias.Api.Features.Fluxos;
using RadarTendencias.Api.Features.Temporadas;
using RadarTendencias.Api.Features.Calendario;
using RadarTendencias.Api.Features.Pesquisa;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddHttpClient("JikanClient", client =>
{
    client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
});

builder.Services.AddHttpClient("TmdbClient", client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["TmdbApiKey"]}");
});

builder.Services.AddHttpClient("RedditClient", client =>
{
    client.BaseAddress = new Uri("https://www.reddit.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 RadarTendenciasBot/1.0");
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
EnsureDatabase.For.SqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

upgrader.PerformUpgrade();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();
app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = @"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StreamingProviders')
        BEGIN
            CREATE TABLE StreamingProviders (
                ProviderID INT IDENTITY(1,1) PRIMARY KEY,
                FranquiaID INT NOT NULL FOREIGN KEY REFERENCES Franquias(FranquiaID) ON DELETE CASCADE,
                NomeProvider VARCHAR(100) NOT NULL,
                LogoUrl VARCHAR(500) NULL,
                Tipo VARCHAR(50) NULL
            );
        END
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Estudios')
        BEGIN
            CREATE TABLE Estudios (
                EstudioID INT IDENTITY(1,1) PRIMARY KEY,
                FranquiaID INT NOT NULL FOREIGN KEY REFERENCES Franquias(FranquiaID) ON DELETE CASCADE,
                NomeEstudio VARCHAR(150) NOT NULL
            );
        END";
    await connection.ExecuteAsync(sql);
}

using (var scope2 = app.Services.CreateScope())
{
    var config2 = scope2.ServiceProvider.GetRequiredService<IConfiguration>();
    using var connection2 = new SqlConnection(config2.GetConnectionString("DefaultConnection"));
    var sqlCheck = @"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'MonitoramentoHype') AND name = 'ResumoIA')
        BEGIN
            ALTER TABLE MonitoramentoHype ADD ResumoIA VARCHAR(MAX) NULL;
        END";
    await connection2.ExecuteAsync(sqlCheck);
}

using (var scope3 = app.Services.CreateScope())
{
    var config = scope3.ServiceProvider.GetRequiredService<IConfiguration>();
    using (var connection = new SqlConnection(config.GetConnectionString("DefaultConnection")))
    {
        var sqlCreateTable = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Favoritos')
            BEGIN
                CREATE TABLE Favoritos (
                    FavoritoID INT IDENTITY(1,1) PRIMARY KEY,
                    FranquiaID INT NOT NULL FOREIGN KEY REFERENCES Franquias(FranquiaID) ON DELETE CASCADE,
                    DataAdicao DATETIME DEFAULT GETDATE(),
                    CONSTRAINT UQ_Favoritos_Franquia UNIQUE (FranquiaID)
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notificacoes')
            BEGIN
                CREATE TABLE Notificacoes (
                    NotificacaoID INT IDENTITY(1,1) PRIMARY KEY,
                    FranquiaID INT NULL FOREIGN KEY REFERENCES Franquias(FranquiaID) ON DELETE SET NULL,
                    Titulo VARCHAR(200) NOT NULL,
                    Mensagem VARCHAR(MAX) NOT NULL,
                    Lida BIT DEFAULT 0,
                    DataCriacao DATETIME DEFAULT GETDATE()
                );
            END";
        await connection.ExecuteAsync(sqlCreateTable);
    }
}

app.MapGet("/franquias", async (IMediator mediator) => await mediator.Send(new GetFranquiasQuery()));
app.MapGet("/monitoramento/dashboard", async (IMediator mediator) => await mediator.Send(new GetMonitoramentoDashboardQuery()));
app.MapPost("/franquias/sync", async (IMediator mediator, [AsParameters] PostFranquiasSyncQuery request) => await mediator.Send(request));
app.MapGet("/pesquisa", async (IMediator mediator, [AsParameters] GetPesquisaQuery request) => await mediator.Send(request));
app.MapGet("/franquias/{id}/detalhes", async (IMediator mediator, [AsParameters] GetFranquiasIdDetalhesQuery request) => await mediator.Send(request));
app.MapGet("/franquias/{id}/personagens", async (IMediator mediator, [AsParameters] GetFranquiasIdPersonagensQuery request) => await mediator.Send(request));
app.MapGet("/franquias/{id}/comunidade", async (IMediator mediator, [AsParameters] GetFranquiasIdComunidadeQuery request) => await mediator.Send(request));
app.MapPost("/monitoramento", async (IMediator mediator, [AsParameters] PostMonitoramentoQuery request) => await mediator.Send(request));
app.MapGet("/fluxos", async (IMediator mediator) => await mediator.Send(new GetFluxosQuery()));
app.MapPost("/fluxos", async (IMediator mediator, [AsParameters] PostFluxosQuery request) => await mediator.Send(request));
app.MapGet("/alertas", async (IMediator mediator) => await mediator.Send(new GetAlertasQuery()));
app.MapPost("/alertas", async (IMediator mediator, [AsParameters] PostAlertasQuery request) => await mediator.Send(request));
app.MapPut("/alertas/{id}/ler", async (IMediator mediator, [AsParameters] PutAlertasIdLerQuery request) => await mediator.Send(request));
app.MapGet("/favoritos", async (IMediator mediator) => await mediator.Send(new GetFavoritosQuery()));
app.MapPost("/favoritos/toggle/{franquiaId}", async (IMediator mediator, [AsParameters] PostFavoritosToggleFranquiaidQuery request) => await mediator.Send(request));
app.MapGet("/favoritos/check/{franquiaId}", async (IMediator mediator, [AsParameters] GetFavoritosCheckFranquiaidQuery request) => await mediator.Send(request));
app.MapGet("/notificacoes", async (IMediator mediator) => await mediator.Send(new GetNotificacoesQuery()));
app.MapPatch("/notificacoes/{id}/ler", async (IMediator mediator, [AsParameters] PatchNotificacoesIdLerQuery request) => await mediator.Send(request));
app.MapDelete("/notificacoes/{id}", async (IMediator mediator, [AsParameters] DeleteNotificacoesIdQuery request) => await mediator.Send(request));
app.MapGet("/franquias/{id}/comparativo-regional", async (IMediator mediator, [AsParameters] GetFranquiasIdComparativoRegionalQuery request) => await mediator.Send(request));
app.MapGet("/franquias/{id}/streaming", async (IMediator mediator, [AsParameters] GetFranquiasIdStreamingQuery request) => await mediator.Send(request));
app.MapGet("/franquias/{id}/estudios", async (IMediator mediator, [AsParameters] GetFranquiasIdEstudiosQuery request) => await mediator.Send(request));
app.MapGet("/calendario/semana", async (IMediator mediator) => await mediator.Send(new GetCalendarioSemanaQuery()));
app.MapGet("/temporadas/analise", async (IMediator mediator, [AsParameters] GetTemporadasAnaliseQuery request) => await mediator.Send(request));
app.MapGet("/franquias/{id}/relacoes", async (IMediator mediator, [AsParameters] GetFranquiasIdRelacoesQuery request) => await mediator.Send(request));

app.Run();
