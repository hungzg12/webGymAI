using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class WorkoutPlanDetail
{
    public long PlanDetailId
    {
        get; set;
    }

    public long PlanId
    {
        get; set;
    }

    public long ExerciseId
    {
        get; set;
    }

    public int? Sets
    {
        get; set;
    }

    public int? Reps
    {
        get; set;
    }

    public decimal? Weight
    {
        get; set;
    }

    public int? RestTime
    {
        get; set;
    }

    public int? OrderNo
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

    public virtual Exercise Exercise { get; set; } = null!;

    public virtual WorkoutPlan Plan { get; set; } = null!;
}
