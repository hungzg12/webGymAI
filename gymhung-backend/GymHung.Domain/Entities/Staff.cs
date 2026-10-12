using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Staff
{
    public long StaffId
    {
        get; set;
    }

    public long UserId
    {
        get; set;
    }

    public string? Position
    {
        get; set;
    }

    public DateOnly? HireDate
    {
        get; set;
    }

    public string? Notes
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt
    {
        get; set;
    }

    public DateTime? UpdatedAt
    {
        get; set;
    }

    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    public virtual User User { get; set; } = null!;
}
