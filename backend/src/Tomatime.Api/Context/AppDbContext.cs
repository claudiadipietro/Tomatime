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
	public DbSet<RewardOperation> RewardOperations => Set<RewardOperation>();
	public DbSet<PushDevice> PushDevices => Set<PushDevice>();
	public DbSet<ReminderDelivery> ReminderDeliveries => Set<ReminderDelivery>();

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

			entity.HasOne<Theme>()
				.WithMany()
				.HasForeignKey(reward => reward.ThemeId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasData(
				new Reward { Id = RewardCatalog.Pink, Name = "pink", Description = "Pink theme", Cost = 5, Type = RewardType.Theme, ThemeId = RewardCatalog.Pink },
				new Reward { Id = RewardCatalog.Blue, Name = "blue", Description = "Blue theme", Cost = 5, Type = RewardType.Theme, ThemeId = RewardCatalog.Blue },
				new Reward { Id = RewardCatalog.ExtraBreak, Name = "extra-break", Description = "Five extra minutes of break", Cost = 5, Type = RewardType.ExtraBreak });
		});

		modelBuilder.Entity<Theme>().HasData(
			new Theme { Id = RewardCatalog.Pink, Name = "pink" },
			new Theme { Id = RewardCatalog.Blue, Name = "blue" });

		modelBuilder.Entity<Session>(entity =>
		{
			entity.HasKey(session => session.Id);
			entity.Property(session => session.Status).HasConversion<string>();

			entity.HasOne<User>()
				.WithMany()
				.HasForeignKey(session => session.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<UserReward>(entity =>
		{
			entity.HasKey(userReward => userReward.Id);
			entity.HasIndex(userReward => new { userReward.UserId, userReward.RewardId })
				.IsUnique();

			entity.HasOne<User>()
				.WithMany()
				.HasForeignKey(userReward => userReward.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne<Reward>()
				.WithMany()
				.HasForeignKey(userReward => userReward.RewardId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<PasswordResetToken>(entity =>
		{
			entity.HasKey(token => token.Id);
			entity.HasIndex(token => token.TokenHash).IsUnique();
			entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();

			entity.HasOne<User>()
				.WithMany()
				.HasForeignKey(token => token.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		modelBuilder.Entity<RewardOperation>(entity =>
		{
			entity.HasKey(operation => operation.Id);
			entity.Property(operation => operation.Kind).HasMaxLength(32).IsRequired();
			entity.HasOne<User>().WithMany().HasForeignKey(operation => operation.UserId).OnDelete(DeleteBehavior.Cascade);
			entity.HasOne<Reward>().WithMany().HasForeignKey(operation => operation.RewardId).OnDelete(DeleteBehavior.Restrict);
		});
		modelBuilder.Entity<PushDevice>(entity =>
		{
			entity.HasKey(device => device.Id);
			entity.Property(device => device.Endpoint).HasMaxLength(2048).IsRequired();
			entity.Property(device => device.P256dh).HasMaxLength(128).IsRequired();
			entity.Property(device => device.Auth).HasMaxLength(128).IsRequired();
			entity.Property(device => device.Language).HasMaxLength(2).IsRequired();
			entity.HasIndex(device => device.Endpoint).IsUnique();
			entity.HasOne<User>().WithMany().HasForeignKey(device => device.UserId).OnDelete(DeleteBehavior.Cascade);
		});
		modelBuilder.Entity<ReminderDelivery>(entity =>
		{
			entity.HasKey(delivery => new { delivery.SessionId, delivery.PushDeviceId });
			entity.HasOne<Session>().WithMany().HasForeignKey(delivery => delivery.SessionId).OnDelete(DeleteBehavior.Cascade);
			entity.HasOne<PushDevice>().WithMany().HasForeignKey(delivery => delivery.PushDeviceId).OnDelete(DeleteBehavior.Cascade);
		});
	}
}
