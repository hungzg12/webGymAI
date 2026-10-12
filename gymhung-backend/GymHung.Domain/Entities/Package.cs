using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Package
{
    public long PackageId
    {
        get; set;
    }

    public string PackageName { get; set; } = null!;

    public string? Description
    {
        get; set;
    }

    public int? DurationDays
    {
        get; set;
    }

    public int? TotalSessions
    {
        get; set;
    }

    public decimal Price
    {
        get; set;
    }

    public string? Benefits
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

    public virtual ICollection<MemberPackage> MemberPackages { get; set; } = new List<MemberPackage>();
}
