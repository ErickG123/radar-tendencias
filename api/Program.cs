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
using Microsoft.Extensions.Caching.Distributed;
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
app.MapGet("/api/calendario/semana", async (IMediator mediator) => await mediator.Send(new GetCalendarioSemanaQuery()));
app.MapGet("/temporadas/analise", async (IMediator mediator, [AsParameters] GetTemporadasAnaliseQuery request) => await mediator.Send(request));
app.MapGet("/franquias/{id}/relacoes", async (IMediator mediator, [AsParameters] GetFranquiasIdRelacoesQuery request) => await mediator.Send(request));

using (var scope4 = app.Services.CreateScope())
{
    var config = scope4.ServiceProvider.GetRequiredService<IConfiguration>();
    using (var connection = new SqlConnection(config.GetConnectionString("DefaultConnection")))
    {
        var sqlSentimento = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EpisodiosSentimento')
            BEGIN
                CREATE TABLE EpisodiosSentimento (
                    EpisodioID BIGINT IDENTITY(1,1) PRIMARY KEY,
                    FranquiaID INT NOT NULL FOREIGN KEY REFERENCES Franquias(FranquiaID) ON DELETE CASCADE,
                    Temporada INT NOT NULL,
                    Episodio INT NOT NULL,
                    DataExibicao DATETIME,
                    NotaPublico DECIMAL(3,1),
                    SentimentoAnalise DECIMAL(5,2),
                    CONSTRAINT UQ_Episodio UNIQUE (FranquiaID, Temporada, Episodio)
                );
            END";
        await connection.ExecuteAsync(sqlSentimento);

        var sqlImpacto = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImpactoComercial')
            BEGIN
                CREATE TABLE ImpactoComercial (
                    ImpactoID BIGINT IDENTITY(1,1) PRIMARY KEY,
                    FranquiaID INT NOT NULL FOREIGN KEY REFERENCES Franquias(FranquiaID) ON DELETE CASCADE,
                    VolumeProdutosAmazon INT DEFAULT 0,
                    VolumeColecionaveis INT DEFAULT 0,
                    PrecoMedio DECIMAL(10,2) DEFAULT 0,
                    DataAtualizacao DATETIME NOT NULL DEFAULT GETDATE()
                );
            END";
        await connection.ExecuteAsync(sqlImpacto);
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

app.MapGet("/api/telemetria/status", async (IConfiguration config, [Microsoft.AspNetCore.Mvc.FromServices] Microsoft.Extensions.Caching.Distributed.IDistributedCache cache) =>
{
    int totalFranquias = 0;
    string dbStatus = "Offline";
    DateTime? ultimaSinc = null;
    
    try 
    {
        using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        totalFranquias = await connection.QuerySingleOrDefaultAsync<int>("SELECT COUNT(*) FROM Franquias");
        ultimaSinc = await connection.QuerySingleOrDefaultAsync<DateTime?>("SELECT TOP 1 DataMedicao FROM MonitoramentoHype ORDER BY DataMedicao DESC");
        dbStatus = "Online";
    } 
    catch 
    {
        dbStatus = "Offline";
        totalFranquias = 0;
    }

    string redisStatus = "Offline";
    try 
    {
        await cache.SetStringAsync("dummy_telemetria", "1", new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) });
        redisStatus = "Online";
    }
    catch
    {
        redisStatus = "Offline";
    }

    return Results.Ok(new {
        apiStatus = "Online",
        memoriaUsadaMb = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024),
        databaseStatus = dbStatus,
        totalFranquiasMonitoradas = totalFranquias,
        redisStatus = redisStatus,
        ultimaSincronizacaoWorker = ultimaSinc
    });
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

        var sqlProgramacao = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProgramacaoSemanal')
            BEGIN
                CREATE TABLE ProgramacaoSemanal (
                    ProgramacaoID INT IDENTITY(1,1) PRIMARY KEY,
                    FranquiaID INT NOT NULL FOREIGN KEY REFERENCES Franquias(FranquiaID) ON DELETE CASCADE,
                    DiaSemana INT NOT NULL, 
                    HorarioEmissao TIME NOT NULL, 
                    EpisodioAtual VARCHAR(50) NULL
                );
            END";
        await connection.ExecuteAsync(sqlProgramacao);

        var sqlConfiguracoes = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConfiguracoesWorker')
            BEGIN
                CREATE TABLE ConfiguracoesWorker (
                    Id INT PRIMARY KEY,
                    ScraperHabilitado BIT NOT NULL,
                    IntervaloBaseMinutos INT NOT NULL,
                    ModoTurboHabilitado BIT NOT NULL,
                    IntervaloPromocionalMinutos INT NOT NULL,
                    ForcarExecucao BIT NOT NULL DEFAULT 0
                );
            END
            ELSE
            BEGIN
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ConfiguracoesWorker') AND name = 'ForcarExecucao')
                BEGIN
                    ALTER TABLE ConfiguracoesWorker ADD ForcarExecucao BIT NOT NULL DEFAULT 0;
                END
            END";
        await connection.ExecuteAsync(sqlConfiguracoes);
        
        var sqlWorkerLogs = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkerLogs')
            BEGIN
                CREATE TABLE WorkerLogs (
                    LogID INT IDENTITY(1,1) PRIMARY KEY,
                    DataExecucao DATETIME NOT NULL,
                    Status VARCHAR(50) NOT NULL,
                    ItensProcessados INT NOT NULL DEFAULT 0,
                    MensagemErro NVARCHAR(MAX) NULL,
                    DetalhesJson NVARCHAR(MAX) NULL
                );
            END
            ELSE
            BEGIN
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'WorkerLogs') AND name = 'DetalhesJson')
                    ALTER TABLE WorkerLogs ADD DetalhesJson NVARCHAR(MAX) NULL;
            END";
        await connection.ExecuteAsync(sqlWorkerLogs);
        
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

app.MapGet("/api/workers/logs", async (IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = "SELECT TOP 50 LogID, DataExecucao, Status, ItensProcessados, MensagemErro, DetalhesJson FROM WorkerLogs ORDER BY DataExecucao DESC";
    var result = await connection.QueryAsync<WorkerLogDto>(sql);
    
    var mappedResult = result.Select(r => new {
        logID = r.LogID,
        dataExecucao = r.DataExecucao,
        status = r.Status,
        itensProcessados = r.ItensProcessados,
        mensagemErro = r.MensagemErro,
        detalhesJson = r.DetalhesJson
    });
    
    return Results.Ok(mappedResult);
});

app.MapPost("/api/workers/trigger", async (IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    
    // Sinaliza o Worker para acordar imediatamente
    var sql = "UPDATE ConfiguracoesWorker SET ForcarExecucao = 1 WHERE Id = 1";
    await connection.ExecuteAsync(sql);
    
    return Results.Ok(new { message = "Worker acionado com sucesso" });
});

app.MapGet("/api/workers/config", async (IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = "SELECT ScraperHabilitado, IntervaloBaseMinutos, ModoTurboHabilitado, IntervaloPromocionalMinutos FROM ConfiguracoesWorker WHERE Id = 1";
    var result = await connection.QuerySingleOrDefaultAsync<ConfiguracoesWorkerDto>(sql);
    
    if (result == null)
    {
        return Results.Ok(new { 
            scraperHabilitado = false, 
            intervaloBaseMinutos = 240, 
            modoTurboHabilitado = false, 
            intervaloPromocionalMinutos = 0 
        });
    }
    
    return Results.Ok(new {
        scraperHabilitado = result.ScraperHabilitado,
        intervaloBaseMinutos = result.IntervaloBaseMinutos,
        modoTurboHabilitado = result.ModoTurboHabilitado,
        intervaloPromocionalMinutos = result.IntervaloPromocionalMinutos
    });
});

app.MapPost("/api/workers/config", async (ConfiguracoesWorkerDto dto, IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    await connection.OpenAsync();
    using var transaction = connection.BeginTransaction();
    
    try
    {
        var sqlUpsert = @"
            IF EXISTS (SELECT 1 FROM ConfiguracoesWorker WHERE Id = 1)
            BEGIN
                UPDATE ConfiguracoesWorker 
                SET ScraperHabilitado = @ScraperHabilitado, 
                    IntervaloBaseMinutos = @IntervaloBaseMinutos, 
                    ModoTurboHabilitado = @ModoTurboHabilitado, 
                    IntervaloPromocionalMinutos = @IntervaloPromocionalMinutos
                WHERE Id = 1;
            END
            ELSE
            BEGIN
                INSERT INTO ConfiguracoesWorker (Id, ScraperHabilitado, IntervaloBaseMinutos, ModoTurboHabilitado, IntervaloPromocionalMinutos)
                VALUES (1, @ScraperHabilitado, @IntervaloBaseMinutos, @ModoTurboHabilitado, @IntervaloPromocionalMinutos);
            END";
            
        await connection.ExecuteAsync(sqlUpsert, dto, transaction);
        transaction.Commit();
        return Results.Ok();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
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
public record ConfiguracoesWorkerDto(bool ScraperHabilitado, int IntervaloBaseMinutos, bool ModoTurboHabilitado, int IntervaloPromocionalMinutos);
public record WorkerLogDto(int LogID, DateTime DataExecucao, string Status, int ItensProcessados, string MensagemErro, string DetalhesJson);
