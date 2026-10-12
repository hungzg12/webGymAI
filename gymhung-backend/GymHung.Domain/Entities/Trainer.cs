using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Trainer
{
    public long TrainerId
    {
        get; set;
    }

    public long UserId
    {
        get; set;
    }

    public string? Specialization
    {
        get; set;
    }

    public int? ExperienceYears
    {
        get; set;
    }

    public string? Bio
    {
        get; set;
    }

    public string? Certificate
    {
        get; set;
    }

    public decimal? Rating
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt
    {
        get; set;
    }

    public DateTime? UpdatedAt
    {
        get; set;
    }

    public virtual ICollection<MemberTrainer> MemberTrainers { get; set; } = new List<MemberTrainer>();

    public virtual ICollection<TrainerSchedule> TrainerSchedules { get; set; } = new List<TrainerSchedule>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();

    public virtual ICollection<WorkoutSchedule> WorkoutSchedules { get; set; } = new List<WorkoutSchedule>();
}
