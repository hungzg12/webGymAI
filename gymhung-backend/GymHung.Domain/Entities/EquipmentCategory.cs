using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class EquipmentCategory
{
    public long CategoryId
    {
        get; set;
    }

    public string CategoryName { get; set; } = null!;

    public string? Description
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}
