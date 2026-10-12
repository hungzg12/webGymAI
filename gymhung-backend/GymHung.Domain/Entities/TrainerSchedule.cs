using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class TrainerSchedule
{
    public long TrainerScheduleId
    {
        get; set;
    }

    public long TrainerId
    {
        get; set;
    }

    public DateOnly WorkDate
    {
        get; set;
    }

    public TimeOnly StartTime
    {
        get; set;
    }

    public TimeOnly EndTime
    {
        get; set;
    }

    public bool IsAvailable
    {
        get; set;
    }

    public string? Note
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual Trainer Trainer { get; set; } = null!;
}
