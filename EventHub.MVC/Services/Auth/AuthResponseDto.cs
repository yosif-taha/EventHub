namespace EventHub.MVC.Services.Auth;

public sealed class AuthResponseDto
{
    public string Id { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
}
