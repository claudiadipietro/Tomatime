using Tomatime.Api.Models;

namespace Tomatime.Api.DTOs;

public sealed record CompletionResult(CompletionStatus Status, SessionCompletion? Completion = null);
