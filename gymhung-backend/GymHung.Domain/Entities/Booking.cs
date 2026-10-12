using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Booking
{
    public long BookingId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public long WorkoutScheduleId
    {
        get; set;
    }

    public DateTime BookingTime
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

    public virtual Attendance? Attendance
    {
        get; set;
    }

    public virtual Member Member { get; set; } = null!;

    public virtual WorkoutSchedule WorkoutSchedule { get; set; } = null!;
}
