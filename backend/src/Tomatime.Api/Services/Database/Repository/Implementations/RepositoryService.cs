using Tomatime.Api.Context;

namespace Tomatime.Api.Services;

public sealed class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    private readonly AppDbContext _dbContext;

    public Repository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<TEntity> Query()
    {
        return _dbContext.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(
        object id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _dbContext.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        _dbContext.Set<TEntity>().Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
