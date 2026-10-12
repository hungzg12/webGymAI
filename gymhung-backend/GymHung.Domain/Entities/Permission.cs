using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Permission
{
    public long PermissionId
    {
        get; set;
    }

    public string PermissionCode { get; set; } = null!;

    public string PermissionName { get; set; } = null!;

    public string? Description
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
