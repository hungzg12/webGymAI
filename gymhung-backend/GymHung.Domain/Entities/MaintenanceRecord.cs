using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class MaintenanceRecord
{
    public long MaintenanceId
    {
        get; set;
    }

    public long EquipmentId
    {
        get; set;
    }

    public long? StaffId
    {
        get; set;
    }

    public DateOnly MaintenanceDate
    {
        get; set;
    }

    public string? MaintenanceType
    {
        get; set;
    }

    public string? Description
    {
        get; set;
    }

    public decimal? Cost
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual Staff? Staff
    {
        get; set;
    }
}
