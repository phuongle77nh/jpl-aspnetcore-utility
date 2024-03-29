using System.Data;
using Dapper;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using JPL.NetCoreUtility.Application.Common.Persistence;
using JPL.NetCoreUtility.Domain.Common.Contracts;
using JPL.NetCoreUtility.Infrastructure.Persistence.Context;

namespace JPL.NetCoreUtility.Infrastructure.Persistence.Repository;

public class DapperRepository : IDapperRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DapperRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        (await _dbContext.Connection.QueryAsync<T>(sql, param, transaction))
            .AsList();

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity
    {
        if (_dbContext.Model.GetMultiTenantEntityTypes().Any(t => t.ClrType == typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.TenantInfo.Id);
        }

        return await _dbContext.Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);
    }

    public Task<T> QuerySingleAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity
    {
        if (_dbContext.Model.GetMultiTenantEntityTypes().Any(t => t.ClrType == typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.TenantInfo.Id);
        }

        return _dbContext.Connection.QuerySingleAsync<T>(sql, param, transaction);
    }

    public async Task<List<T>> ExecStoredProcAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class
    {
        var result = await _dbContext.Connection.QueryAsync<T>(sql, param, transaction, commandType: CommandType.StoredProcedure);
        return result == null ? new List<T>() : result.ToList();
    }
}