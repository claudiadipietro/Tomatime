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
	public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(user => user.Id);
			entity.HasIndex(user => user.Email).IsUnique();
			entity.Property(user => user.Name).HasMaxLength(100).IsRequired();
			entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
			entity.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
			entity.Property(user => user.CurrentTheme).HasMaxLength(100).IsRequired();
			entity.ToTable("Users", table => table.HasCheckConstraint(
				"CK_Users_PomoBalance_NonNegative",
				"\"PomoBalance\" >= 0"));
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
				.WithMany()
				.HasForeignKey(userReward => userReward.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(userReward => userReward.Reward)
				.WithMany()
				.HasForeignKey(userReward => userReward.RewardId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<PasswordResetToken>(entity =>
		{
			entity.HasKey(token => token.Id);
			entity.HasIndex(token => token.TokenHash).IsUnique();
			entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();

			entity.HasOne(token => token.User)
				.WithMany(user => user.PasswordResetTokens)
				.HasForeignKey(token => token.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});
	}
}
