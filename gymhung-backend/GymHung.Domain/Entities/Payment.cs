using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Payment
{
    public long PaymentId
    {
        get; set;
    }

    public long MemberPackageId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public decimal Amount
    {
        get; set;
    }

    public string? PaymentMethod
    {
        get; set;
    }

    public string? TransactionCode
    {
        get; set;
    }

    public DateTime? PaidAt
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public string? Provider
    {
        get; set;
    }

    public string? Notes
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual Member Member { get; set; } = null!;

    public virtual MemberPackage MemberPackage { get; set; } = null!;

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
