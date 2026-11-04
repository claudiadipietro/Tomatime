using Tomatime.Api.Context;

namespace Tomatime.Api.Services;

public sealed class Repository<TEntity>(AppDbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    public IQueryable<TEntity> Query()
    {
        return dbContext.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(
        object id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        dbContext.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        dbContext.Set<TEntity>().Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
