using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class PaymentTransaction
{
    public long PaymentTransactionId
    {
        get; set;
    }

    public long PaymentId
    {
        get; set;
    }

    public string Provider { get; set; } = null!;

    public string? TransactionCode
    {
        get; set;
    }

    public string? RequestData
    {
        get; set;
    }

    public string? ResponseData
    {
        get; set;
    }

    public decimal? Amount
    {
        get; set;
    }

    public string? TransactionStatus
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual Payment Payment { get; set; } = null!;
}
