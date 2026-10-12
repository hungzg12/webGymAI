using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Role
{
    public long RoleId
    {
        get; set;
    }

    public string RoleName { get; set; } = null!;

    public string? Description
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
