using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class WorkoutResult
{
    public long ResultId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public long ExerciseId
    {
        get; set;
    }

    public long? PlanId
    {
        get; set;
    }

    public DateOnly WorkoutDate
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

    public int? DurationMinutes
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

    public virtual Member Member { get; set; } = null!;

    public virtual WorkoutPlan? Plan
    {
        get; set;
    }
}
