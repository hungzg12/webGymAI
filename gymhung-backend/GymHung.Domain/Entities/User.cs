using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class User
{
    public long UserId
    {
        get; set;
    }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone
    {
        get; set;
    }

    public string? AvatarUrl
    {
        get; set;
    }

    public string? Gender
    {
        get; set;
    }

    public DateOnly? DateOfBirth
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public DateTime? LastLoginAt
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public DateTime? UpdatedAt
    {
        get; set;
    }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual Member? Member
    {
        get; set;
    }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Staff? Staff
    {
        get; set;
    }

    public virtual Trainer? Trainer
    {
        get; set;
    }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
