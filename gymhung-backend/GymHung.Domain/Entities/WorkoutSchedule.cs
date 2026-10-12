using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class WorkoutSchedule
{
    public long WorkoutScheduleId
    {
        get; set;
    }

    public long TrainerId
    {
        get; set;
    }

    public long? RoomId
    {
        get; set;
    }

    public long? PlanId
    {
        get; set;
    }

    public string? Title
    {
        get; set;
    }

    public DateOnly ScheduleDate
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

    public int? Capacity
    {
        get; set;
    }

    public int CurrentParticipants
    {
        get; set;
    }

    public string? Location
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

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual WorkoutPlan? Plan
    {
        get; set;
    }

    public virtual Room? Room
    {
        get; set;
    }

    public virtual Trainer Trainer { get; set; } = null!;
}
