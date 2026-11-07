using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Models;
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

    [HttpPost]
    public async Task<IActionResult> CreateTheme([FromBody] Theme theme)
    {
        var createdTheme = await _themeDatabaseService.CreateThemeAsync(theme);
        if (createdTheme == null)
        {
            return BadRequest();
        }
        return CreatedAtAction(nameof(GetThemeById), new { themeId = createdTheme.Id }, createdTheme);
    }

    [HttpPut("{themeId}")]
    public async Task<IActionResult> UpdateTheme(int themeId, [FromBody] Theme theme)
    {
        if (themeId != theme.Id)
        {
            return BadRequest();
        }

        var updatedTheme = await _themeDatabaseService.UpdateThemeAsync(theme);
        if (updatedTheme == null)
        {
            return NotFound();
        }
        return Ok(updatedTheme);
    }

    [HttpDelete("{themeId}")]
    public async Task<IActionResult> DeleteTheme(int themeId)
    {
        var deleted = await _themeDatabaseService.DeleteThemeAsync(themeId);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllThemes()
    {
        var themes = await _themeDatabaseService.GetAllThemesAsync();
        return Ok(themes);
    }
}