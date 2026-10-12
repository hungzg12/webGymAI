using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class MemberTrainer
{
    public long MemberTrainerId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public long TrainerId
    {
        get; set;
    }

    public DateOnly? StartDate
    {
        get; set;
    }

    public DateOnly? EndDate
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

    public virtual Trainer Trainer { get; set; } = null!;
}
