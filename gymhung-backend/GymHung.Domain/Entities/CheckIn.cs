using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class CheckIn
{
    public long CheckInId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public DateTime CheckInTime
    {
        get; set;
    }

    public DateTime? CheckOutTime
    {
        get; set;
    }

    public string? Method
    {
        get; set;
    }

    public string? SourceDevice
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public string? Note
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual Member Member { get; set; } = null!;
}
