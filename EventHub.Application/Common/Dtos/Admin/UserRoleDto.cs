namespace EventHub.Application.Common.Dtos.Admin
{
    public record UserRoleDto(
        Guid Id,
        string Email,
        string FullName,
        string Role);
}
