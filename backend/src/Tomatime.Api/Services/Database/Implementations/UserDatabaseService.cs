using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class UserDatabaseService : IUserDatabaseService
{
    private readonly IRepository<User> _repository;

    public UserDatabaseService(IRepository<User> repository)
    {
        _repository = repository;
    }
    public Task<User?> GetUserByIdAsync(int userId)
    {
        return _repository.GetByIdAsync(userId);
    }

    public Task<User?> GetUserByEmailAsync(string email)
    {
        return _repository.Query()
            .SingleOrDefaultAsync(user => user.Email == email);
    }

    public async Task<User?> CreateUserAsync(User user)
    {
        await _repository.AddAsync(user);
        await _repository.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateUserAsync(User user)
    {
        var existingUser = await _repository.GetByIdAsync(user.Id);

        if (existingUser is null)
        {
            return null;
        }

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        existingUser.PasswordHash = user.PasswordHash;
        existingUser.PomoBalance = user.PomoBalance;
        existingUser.CurrentTheme = user.CurrentTheme;

        await _repository.SaveChangesAsync();
        return existingUser;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _repository.GetByIdAsync(userId);

        if (user is null)
        {
            return false;
        }

        _repository.Remove(user);
        await _repository.SaveChangesAsync();
        return true;
    }
}