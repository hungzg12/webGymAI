using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Equipment
{
    public long EquipmentId
    {
        get; set;
    }

    public long? CategoryId
    {
        get; set;
    }

    public string EquipmentName { get; set; } = null!;

    public string? Brand
    {
        get; set;
    }

    public string? SerialNumber
    {
        get; set;
    }

    public string? Location
    {
        get; set;
    }

    public DateOnly? PurchaseDate
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public string? Notes
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

    public virtual EquipmentCategory? Category
    {
        get; set;
    }

    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}
