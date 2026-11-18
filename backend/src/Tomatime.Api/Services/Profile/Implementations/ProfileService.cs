using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tomatime.Api.Context;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public ProfileService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public Task<ProfileInfo?> GetAsync(int userId) => _db.Users.AsNoTracking()
        .Where(user => user.Id == userId)
        .Select(user => new ProfileInfo(user.Name, user.Email, user.PomoBalance, user.CurrentTheme, user.NotificationsEnabled))
        .SingleOrDefaultAsync();

    public async Task<bool> UpdateNotificationsAsync(int userId, bool enabled) =>
        await _db.Users.Where(user => user.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.NotificationsEnabled, enabled)) == 1;

    public async Task<PasswordChangeStatus> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        await using IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync();
        
        User? user = await _db.Users.FromSqlInterpolated(
            $"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        if (user is null)
        {
            return PasswordChangeStatus.UserNotFound;
        }

        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            return PasswordChangeStatus.IncorrectPassword;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        
        await _db.PasswordResetTokens.Where(token => token.UserId == userId && token.UsedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.UsedAt, DateTimeOffset.UtcNow));
        
        await _db.SaveChangesAsync();
        
        await transaction.CommitAsync();
        
        return PasswordChangeStatus.Success;
    }
}
