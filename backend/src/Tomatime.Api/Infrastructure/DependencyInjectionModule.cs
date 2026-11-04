using Autofac;
using Tomatime.Api.Context;
using Tomatime.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Tomatime.Api.Infrastructure;

public sealed class DependencyInjectionModule(IConfiguration configuration) : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(Repository<>))
            .As(typeof(IRepository<>))
            .InstancePerLifetimeScope();

        builder.Register(context =>
            {
                var connectionString = configuration.GetConnectionString("tomatime")
                    ?? configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException(
                        "Connection string 'tomatime' was not found.");

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;

                return new AppDbContext(options);
            })
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
