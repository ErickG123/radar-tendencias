using RadarTendencias.Api.Infrastructure;
using RadarTendencias.Api.Hubs;
using DbUp;
using Microsoft.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using ClosedXML.Excel;
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

builder.Services.AddInfrastructure(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
EnsureDatabase.For.SqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

upgrader.PerformUpgrade();

var app = builder.Build();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<RadarHub>("/hubs/radar");

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

using (var scope4 = app.Services.CreateScope())
{
    var config = scope4.ServiceProvider.GetRequiredService<IConfiguration>();
    using (var connection = new SqlConnection(config.GetConnectionString("DefaultConnection")))
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EpisodiosSentimento')
            BEGIN
                CREATE TABLE EpisodiosSentimento (
                    EpisodioID INT IDENTITY(1,1) PRIMARY KEY,
                    FranquiaID INT NOT NULL FOREIGN KEY REFERENCES Franquias(FranquiaID) ON DELETE CASCADE,
                    NumeroEpisodio INT NOT NULL,
                    SentimentoScore DECIMAL(5,2) NOT NULL,
                    DataExibicao DATETIME NOT NULL
                );
            END";
        await connection.ExecuteAsync(sql);
    }
}

app.MapGet("/api/franquias/comparar", async (string ids, IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var idList = ids.Split(',').Select(int.Parse).ToList();
    
    var sql = @"
        SELECT f.FranquiaID, f.Nome, f.ImagemUrl, 
               ISNULL((SELECT TOP 1 HypeScore FROM MonitoramentoHype WHERE FranquiaID = f.FranquiaID ORDER BY DataMedicao DESC), 0) as HypeScore,
               ISNULL((SELECT TOP 1 SentimentoPositivo FROM MonitoramentoHype WHERE FranquiaID = f.FranquiaID ORDER BY DataMedicao DESC), 0) as Sentimento
        FROM Franquias f
        WHERE f.FranquiaID IN @Ids";
        
    var result = await connection.QueryAsync<dynamic>(sql, new { Ids = idList });
    return Results.Ok(result);
});

app.MapGet("/api/franquias/{id}/episodios", async (int id, IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = "SELECT NumeroEpisodio, SentimentoScore FROM EpisodiosSentimento WHERE FranquiaID = @Id ORDER BY NumeroEpisodio ASC";
    var result = await connection.QueryAsync<dynamic>(sql, new { Id = id });
    return Results.Ok(result);
});

app.MapGet("/api/franquias/discovery", async (int? minSentimento, int? maxHype, IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = @"
        SELECT f.FranquiaID, f.Nome, f.ImagemUrl, m.HypeScore, m.SentimentoPositivo
        FROM Franquias f
        CROSS APPLY (SELECT TOP 1 HypeScore, SentimentoPositivo FROM MonitoramentoHype WHERE FranquiaID = f.FranquiaID ORDER BY DataMedicao DESC) m
        WHERE (@MinSentimento IS NULL OR m.SentimentoPositivo >= @MinSentimento)
          AND (@MaxHype IS NULL OR m.HypeScore <= @MaxHype)
        ORDER BY m.SentimentoPositivo DESC";
        
    var result = await connection.QueryAsync<dynamic>(sql, new { MinSentimento = minSentimento, MaxHype = maxHype });
    return Results.Ok(result);
});

using (var scope5 = app.Services.CreateScope())
{
    var config = scope5.ServiceProvider.GetRequiredService<IConfiguration>();
    using (var connection = new SqlConnection(config.GetConnectionString("DefaultConnection")))
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'MonitoramentoHype') AND name = 'NuvemPalavras')
            BEGIN
                ALTER TABLE MonitoramentoHype ADD NuvemPalavras NVARCHAR(MAX) NULL;
            END";
        await connection.ExecuteAsync(sql);
    }
}

app.MapGet("/api/franquias/{id}/palavras-chave", async (int id, IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = "SELECT TOP 1 NuvemPalavras FROM MonitoramentoHype WHERE FranquiaID = @Id ORDER BY DataMedicao DESC";
    var result = await connection.QuerySingleOrDefaultAsync<string>(sql, new { Id = id });
    
    if (string.IsNullOrEmpty(result)) return Results.Ok(new List<object>());
    
    return Results.Content(result, "application/json");
});

app.MapGet("/api/telemetria/status", async (IConfiguration config) =>
{
    var telemetry = new TelemetryResponse
    {
        ApiStatus = "Online",
        MemoriaUsadaMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)
    };
    
    try 
    {
        using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        await connection.ExecuteAsync("SELECT 1");
        telemetry.DatabaseStatus = "Online";
        
        telemetry.TotalFranquiasMonitoradas = await connection.QuerySingleOrDefaultAsync<int>("SELECT COUNT(*) FROM Franquias");
        telemetry.UltimaSincronizacaoWorker = await connection.QuerySingleOrDefaultAsync<DateTime?>("SELECT TOP 1 DataMedicao FROM MonitoramentoHype ORDER BY DataMedicao DESC");
    } 
    catch 
    {
        telemetry.DatabaseStatus = "Offline";
    }

    return Results.Ok(telemetry);
});

using (var scope6 = app.Services.CreateScope())
{
    var config = scope6.ServiceProvider.GetRequiredService<IConfiguration>();
    using (var connection = new SqlConnection(config.GetConnectionString("DefaultConnection")))
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AlertasUsuario')
            BEGIN
                CREATE TABLE AlertasUsuario (
                    AlertaID INT IDENTITY(1,1) PRIMARY KEY,
                    FranquiaID INT NOT NULL FOREIGN KEY REFERENCES Franquias(FranquiaID) ON DELETE CASCADE,
                    TipoMetrica VARCHAR(50) NOT NULL, 
                    Condicao VARCHAR(20) NOT NULL, 
                    ValorAlvo DECIMAL(5,2) NOT NULL,
                    Ativo BIT NOT NULL DEFAULT 1,
                    DataCriacao DATETIME NOT NULL DEFAULT GETDATE()
                );
            END";
        await connection.ExecuteAsync(sql);
        
        var sqlUsuarios = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios')
            BEGIN
                CREATE TABLE Usuarios (
                    UsuarioID INT IDENTITY(1,1) PRIMARY KEY,
                    Nome VARCHAR(100) NOT NULL,
                    Email VARCHAR(100) NOT NULL UNIQUE,
                    SenhaHash VARCHAR(255) NOT NULL,
                    DataCriacao DATETIME NOT NULL DEFAULT GETDATE()
                );
            END";
        await connection.ExecuteAsync(sqlUsuarios);

        var sqlAlertas = @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'AlertasUsuario') AND name = 'UsuarioID')
            BEGIN
                ALTER TABLE AlertasUsuario ADD UsuarioID INT NULL FOREIGN KEY REFERENCES Usuarios(UsuarioID) ON DELETE CASCADE;
            END";
        await connection.ExecuteAsync(sqlAlertas);
    }
}

app.MapGet("/api/alertas", async (HttpContext httpContext, IConfiguration config) =>
{
    var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = @"
        SELECT a.AlertaID, a.FranquiaID, f.Nome AS NomeFranquia, a.TipoMetrica, a.Condicao, a.ValorAlvo, a.Ativo 
        FROM AlertasUsuario a
        INNER JOIN Franquias f ON a.FranquiaID = f.FranquiaID
        WHERE a.UsuarioID = @UsuarioID
        ORDER BY a.DataCriacao DESC";
    var result = await connection.QueryAsync<dynamic>(sql, new { UsuarioID = userId });
    return Results.Ok(result);
}).RequireAuthorization();

app.MapPost("/api/alertas", async (CriarAlertaCommand command, HttpContext httpContext, IConfiguration config) =>
{
    var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = @"
        INSERT INTO AlertasUsuario (FranquiaID, TipoMetrica, Condicao, ValorAlvo, Ativo, UsuarioID) 
        VALUES (@FranquiaID, @TipoMetrica, @Condicao, @ValorAlvo, 1, @UsuarioID)";
    await connection.ExecuteAsync(sql, new { command.FranquiaID, command.TipoMetrica, command.Condicao, command.ValorAlvo, UsuarioID = userId });
    return Results.Created("/api/alertas", command);
}).RequireAuthorization();

app.MapDelete("/api/alertas/{id}", async (int id, HttpContext httpContext, IConfiguration config) =>
{
    var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = "DELETE FROM AlertasUsuario WHERE AlertaID = @Id AND UsuarioID = @UsuarioID";
    await connection.ExecuteAsync(sql, new { Id = id, UsuarioID = userId });
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/relatorios/mercado/excel", async (IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = @"
        SELECT f.Nome, m.HypeScore, m.SentimentoPositivo, 
               ISNULL(i.VolumeProdutosAmazon, 0) + ISNULL(i.VolumeColecionaveis, 0) AS TotalProdutos,
               ISNULL(i.PrecoMedio, 0) AS PrecoMedio
        FROM Franquias f
        LEFT JOIN (
            SELECT FranquiaID, HypeScore, SentimentoPositivo,
                   ROW_NUMBER() OVER(PARTITION BY FranquiaID ORDER BY DataMedicao DESC) as rn
            FROM MonitoramentoHype
        ) m ON f.FranquiaID = m.FranquiaID AND m.rn = 1
        LEFT JOIN (
            SELECT FranquiaID, VolumeProdutosAmazon, VolumeColecionaveis, PrecoMedio,
                   ROW_NUMBER() OVER(PARTITION BY FranquiaID ORDER BY DataAtualizacao DESC) as rn
            FROM ImpactoComercial
        ) i ON f.FranquiaID = i.FranquiaID AND i.rn = 1
        ORDER BY m.HypeScore DESC";

    var dados = await connection.QueryAsync<dynamic>(sql);

    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Relatório de Mercado");

    worksheet.Cell(1, 1).Value = "Nome da Franquia";
    worksheet.Cell(1, 2).Value = "Hype Score";
    worksheet.Cell(1, 3).Value = "Sentimento (%)";
    worksheet.Cell(1, 4).Value = "Volume de Produtos (SKUs)";
    worksheet.Cell(1, 5).Value = "Preço Médio (R$)";

    var headerRow = worksheet.Row(1);
    headerRow.Style.Font.Bold = true;
    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

    int row = 2;
    foreach (var item in dados)
    {
        worksheet.Cell(row, 1).Value = item.Nome;
        worksheet.Cell(row, 2).Value = Math.Round((decimal)(item.HypeScore ?? 0), 1);
        worksheet.Cell(row, 3).Value = Math.Round((decimal)(item.SentimentoPositivo ?? 0), 1);
        worksheet.Cell(row, 4).Value = item.TotalProdutos;
        worksheet.Cell(row, 5).Value = item.PrecoMedio;
        row++;
    }

    worksheet.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var content = stream.ToArray();

    return Results.File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"RadarMercado_{DateTime.Now:yyyyMMdd}.xlsx");
});

app.MapPost("/api/auth/registrar", async (RegistrarCommand command, IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var senhaHash = BCrypt.Net.BCrypt.HashPassword(command.Senha);
    var sql = "INSERT INTO Usuarios (Nome, Email, SenhaHash) VALUES (@Nome, @Email, @SenhaHash)";
    await connection.ExecuteAsync(sql, new { command.Nome, command.Email, SenhaHash = senhaHash });
    return Results.Ok();
});

app.MapPost("/api/auth/login", async (LoginCommand command, IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = "SELECT UsuarioID, Nome, Email, SenhaHash FROM Usuarios WHERE Email = @Email";
    var user = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { command.Email });

    if (user == null || !BCrypt.Net.BCrypt.Verify(command.Senha, (string)user.SenhaHash))
        return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "RadarTendenciasSuperSecretKey2026!@#");
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UsuarioID.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Nome)
        }),
        Expires = DateTime.UtcNow.AddHours(8),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { Token = tokenHandler.WriteToken(token), Usuario = new { user.UsuarioID, user.Nome, user.Email } });
});

app.MapFranquiasEndpoints();

app.Run();

public class TelemetryResponse 
{
    public string ApiStatus { get; set; } = "Offline";
    public string DatabaseStatus { get; set; } = "Offline";
    public string RedisStatus { get; set; } = "Online"; 
    public int TotalFranquiasMonitoradas { get; set; }
    public DateTime? UltimaSincronizacaoWorker { get; set; }
    public long MemoriaUsadaMb { get; set; }
}

public record CriarAlertaCommand(int FranquiaID, string TipoMetrica, string Condicao, decimal ValorAlvo);
public record RegistrarCommand(string Nome, string Email, string Senha);
public record LoginCommand(string Email, string Senha);
