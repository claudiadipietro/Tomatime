using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public interface ISessionCalendarService
{
    Task<List<CalendarSession>> GetSessionsAsync(int userId, DateTime from, DateTime to);
}
