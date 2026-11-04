using Microsoft.EntityFrameworkCore;

namespace Tomatime.Api.Context;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
