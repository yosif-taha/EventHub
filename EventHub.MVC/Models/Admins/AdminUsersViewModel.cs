using EventHub.Domin.Enums;
using EventHub.MVC.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace EventHub.MVC.Models.Admins;

public sealed class AdminUsersViewModel
{
    public AdminUserListFilter Filter { get; init; } = new();
    public PaginatedResult<AdminUserDto> Users { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

public sealed class AdminUserListFilter
{
    public int PageNumber { get; set; } = 1;
    public string? SearchValue { get; set; }
}

public sealed class AdminUserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed class UpdateUserRoleViewModel
{
    [Required]
    public Guid UserId { get; set; }

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }
}

public sealed record UpdateUserRoleRequest(UserRole Role);
