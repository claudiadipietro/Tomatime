using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class ProfileService(AppDbContext db, IPasswordHasher<User> passwordHasher) : IProfileService
{
    public Task<ProfileInfo?> GetAsync(int userId) => db.Users.AsNoTracking()
        .Where(user => user.Id == userId)
        .Select(user => new ProfileInfo(user.Name, user.Email, user.PomoBalance, user.CurrentTheme, user.NotificationsEnabled))
        .SingleOrDefaultAsync();

    public async Task<bool> UpdateNotificationsAsync(int userId, bool enabled) =>
        await db.Users.Where(user => user.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.NotificationsEnabled, enabled)) == 1;

    public async Task<PasswordChangeStatus> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = await db.Users.FromSqlInterpolated(
            $"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        if (user is null) return PasswordChangeStatus.UserNotFound;
        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            return PasswordChangeStatus.IncorrectPassword;

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        await db.PasswordResetTokens.Where(token => token.UserId == userId && token.UsedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.UsedAt, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return PasswordChangeStatus.Success;
    }
}
