using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public class ThemeDatabaseService : IThemeDatabaseService
{
    private readonly IRepository<Theme> _repository;

    public ThemeDatabaseService(IRepository<Theme> repository)
    {
        _repository = repository;
    }

    public async Task<Theme?> GetThemeByIdAsync(int themeId)
    {
        return await _repository.GetByIdAsync(themeId);
    }

    public async Task<Theme?> CreateThemeAsync(Theme theme)
    {
        await _repository.AddAsync(theme);
        await _repository.SaveChangesAsync();
        return theme;
    }

    public async Task<Theme?> UpdateThemeAsync(Theme theme)
    {
        var existingTheme = await _repository.GetByIdAsync(theme.Id);

        if (existingTheme is null)
        {
            return null;
        }

        existingTheme.Name = theme.Name;

        await _repository.SaveChangesAsync();
        return existingTheme;
    }

    public async Task<bool> DeleteThemeAsync(int themeId)
    {
        var theme = await _repository.GetByIdAsync(themeId);

        if (theme is null)
        {
            return false;
        }

        _repository.Remove(theme);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<List<Theme>> GetAllThemesAsync()
    {
        return await _repository.Query().ToListAsync();
    }
}