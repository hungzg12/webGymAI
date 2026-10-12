using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class MemberPackage
{
    public long MemberPackageId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public long PackageId
    {
        get; set;
    }

    public DateOnly StartDate
    {
        get; set;
    }

    public DateOnly? EndDate
    {
        get; set;
    }

    public decimal Price
    {
        get; set;
    }

    public int? RemainingSessions
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

    public virtual Member Member { get; set; } = null!;

    public virtual Package Package { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
