using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Exercise
{
    public long ExerciseId
    {
        get; set;
    }

    public string ExerciseName { get; set; } = null!;

    public string? MuscleGroup
    {
        get; set;
    }

    public string? Description
    {
        get; set;
    }

    public string? Instructions
    {
        get; set;
    }

    public string? VideoUrl
    {
        get; set;
    }

    public string? ImageUrl
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual ICollection<WorkoutPlanDetail> WorkoutPlanDetails { get; set; } = new List<WorkoutPlanDetail>();

    public virtual ICollection<WorkoutResult> WorkoutResults { get; set; } = new List<WorkoutResult>();
}
