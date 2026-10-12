using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class RolePermission
{
    public long RoleId
    {
        get; set;
    }

    public long PermissionId
    {
        get; set;
    }

    public DateTime AssignedAt
    {
        get; set;
    }

    public virtual Permission Permission { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
