using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public interface IThemeDatabaseService
{
    Task<Theme?> GetThemeByIdAsync(int themeId);
    Task<Theme?> CreateThemeAsync(Theme theme);
    Task<Theme?> UpdateThemeAsync(Theme theme);
    Task<bool> DeleteThemeAsync(int themeId);
    Task<List<Theme>> GetAllThemesAsync();
}