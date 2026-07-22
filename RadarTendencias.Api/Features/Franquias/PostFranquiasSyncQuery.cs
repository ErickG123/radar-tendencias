using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Franquias;

public class PostFranquiasSyncQuery : IRequest<IResult>
{
    public SyncFranquiaDTO Dto { get; set; }
}

public class PostFranquiasSyncHandler : IRequestHandler<PostFranquiasSyncQuery, IResult>
{
    private readonly IConfiguration _config;

    public PostFranquiasSyncHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(PostFranquiasSyncQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    await connection.OpenAsync();
    using var transaction = connection.BeginTransaction();
    try
    {
        var sqlCheck = "SELECT FranquiaID FROM Franquias WHERE Nome = @Nome";
        var id = await connection.QuerySingleOrDefaultAsync<int?>(sqlCheck, new { request.Dto.Nome }, transaction);
        if (id == null || id == 0)
        {
            var sqlInsert = "INSERT INTO Franquias (Nome, CategoriaID, Ativo, ExternalID, Sinopse, ImagemUrl) OUTPUT INSERTED.FranquiaID VALUES (@Nome, @CategoriaID, 1, @ExternalID, @Sinopse, @ImagemUrl)";
            id = await connection.QuerySingleAsync<int>(sqlInsert, request.Dto, transaction);
        }
        else
        {
            var sqlUpdate = "UPDATE Franquias SET ExternalID = @ExternalID, Sinopse = @Sinopse, ImagemUrl = @ImagemUrl WHERE FranquiaID = @id";
            await connection.ExecuteAsync(sqlUpdate, new { request.Dto.ExternalID, request.Dto.Sinopse, request.Dto.ImagemUrl, id }, transaction);
        }
        foreach (var tagName in request.Dto.Tags)
        {
            var sqlTag = "SELECT TagID FROM Tags WHERE Nome = @Nome";
            var tagId = await connection.QuerySingleOrDefaultAsync<int?>(sqlTag, new { Nome = tagName }, transaction);
            if (tagId == null || tagId == 0)
            {
                var sqlInsertTag = "INSERT INTO Tags (Nome) OUTPUT INSERTED.TagID VALUES (@Nome)";
                tagId = await connection.QuerySingleAsync<int>(sqlInsertTag, new { Nome = tagName }, transaction);
            }
            var sqlRelCheck = "SELECT COUNT(1) FROM FranquiaTags WHERE FranquiaID = @FId AND TagID = @TId";
            var exists = await connection.ExecuteScalarAsync<int>(sqlRelCheck, new { FId = id, TId = tagId }, transaction);
            if (exists == 0)
            {
                var sqlInsertRel = "INSERT INTO FranquiaTags (FranquiaID, TagID) VALUES (@FId, @TId)";
                await connection.ExecuteAsync(sqlInsertRel, new { FId = id, TId = tagId }, transaction);
            }
        }
        transaction.Commit();
        return Results.Ok(new { FranquiaID = id });
    }
    catch
    {
        transaction.Rollback();
        throw;
    }

    }
}
