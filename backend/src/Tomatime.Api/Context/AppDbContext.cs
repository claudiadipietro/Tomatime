using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Models;

namespace Tomatime.Api.Context;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<User> Users => Set<User>();
	public DbSet<Reward> Rewards => Set<Reward>();
	public DbSet<Theme> Themes => Set<Theme>();
	public DbSet<Session> Sessions => Set<Session>();
	public DbSet<UserReward> UserRewards => Set<UserReward>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(user => user.Id);
			entity.HasIndex(user => user.Email).IsUnique();
			entity.Property(user => user.Email).IsRequired();
			entity.Property(user => user.Name).IsRequired();
			entity.Property(user => user.PasswordHash).IsRequired();
		});

		modelBuilder.Entity<Reward>(entity =>
		{
			entity.HasKey(reward => reward.Id);
			entity.Property(reward => reward.Name).IsRequired();
			entity.Property(reward => reward.Description).IsRequired();
			entity.Property(reward => reward.Type).HasConversion<string>();

			entity.HasOne(reward => reward.Theme)
				.WithMany(theme => theme.Rewards)
				.HasForeignKey(reward => reward.ThemeId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<Session>(entity =>
		{
			entity.HasKey(session => session.Id);
			entity.Property(session => session.Status).HasConversion<string>();

			entity.HasOne(session => session.User)
				.WithMany(user => user.Sessions)
				.HasForeignKey(session => session.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserReward>(entity =>
		{
			entity.HasKey(userReward => userReward.Id);
			entity.HasIndex(userReward => new { userReward.UserId, userReward.RewardId })
				.IsUnique();

			entity.HasOne(userReward => userReward.User)
				.WithMany(user => user.UserRewards)
				.HasForeignKey(userReward => userReward.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(userReward => userReward.Reward)
				.WithMany(reward => reward.UserRewards)
				.HasForeignKey(userReward => userReward.RewardId)
				.OnDelete(DeleteBehavior.Cascade);
		});
	}
}
