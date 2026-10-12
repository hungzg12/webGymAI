using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class WorkoutPlan
{
    public long PlanId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public long? TrainerId
    {
        get; set;
    }

    public string PlanName { get; set; } = null!;

    public string? Goal
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

    public DateTime? UpdatedAt
    {
        get; set;
    }

    public virtual Member Member { get; set; } = null!;

    public virtual Trainer? Trainer
    {
        get; set;
    }

    public virtual ICollection<WorkoutPlanDetail> WorkoutPlanDetails { get; set; } = new List<WorkoutPlanDetail>();

    public virtual ICollection<WorkoutResult> WorkoutResults { get; set; } = new List<WorkoutResult>();

    public virtual ICollection<WorkoutSchedule> WorkoutSchedules { get; set; } = new List<WorkoutSchedule>();
}
