using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Route("api/themes")] 
public class ThemeController : ControllerBase
{
    private readonly IThemeDatabaseService _themeDatabaseService;

    public ThemeController(IThemeDatabaseService themeDatabaseService)
    {
        _themeDatabaseService = themeDatabaseService;
    }

    [HttpGet("{themeId}")]
    public async Task<IActionResult> GetThemeById(int themeId)
    {
        var theme = await _themeDatabaseService.GetThemeByIdAsync(themeId);
        if (theme == null)
        {
            return NotFound();
        }
        return Ok(theme);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllThemes()
    {
        var themes = await _themeDatabaseService.GetAllThemesAsync();
        return Ok(themes);
    }
}