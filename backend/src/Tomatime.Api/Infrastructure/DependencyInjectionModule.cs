using Autofac;
using Microsoft.AspNetCore.Identity;
using Tomatime.Api.Context;
using Tomatime.Api.Services;
using Tomatime.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Tomatime.Api.Infrastructure;

public sealed class DependencyInjectionModule(IConfiguration configuration) : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterGeneric(typeof(Repository<>))
            .As(typeof(IRepository<>))
            .InstancePerLifetimeScope();

        builder.RegisterType<UserDatabaseService>()
            .As<IUserDatabaseService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PasswordHasher<User>>()
            .As<IPasswordHasher<User>>()
            .SingleInstance();

        builder.RegisterType<AuthService>()
            .As<IAuthService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PasswordResetEmailSender>()
            .As<IPasswordResetEmailSender>()
            .InstancePerLifetimeScope();

        builder.RegisterType<RewardDatabaseService>()
            .As<IRewardDatabaseService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<SessionDatabaseService>()
            .As<ISessionDatabaseService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<SessionStartService>()
            .As<ISessionStartService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<SessionPlanningService>()
            .As<ISessionPlanningService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<SessionRunService>()
            .As<ISessionRunService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<SessionHistoryService>()
            .As<ISessionHistoryService>()
            .InstancePerLifetimeScope();
        
        builder.RegisterType<ThemeDatabaseService>()
            .As<IThemeDatabaseService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<UserRewardDatabaseService>()
            .As<IUserRewardDatabaseService>()
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
