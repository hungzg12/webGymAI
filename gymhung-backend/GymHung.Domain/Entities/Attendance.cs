using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Attendance
{
    public long AttendanceId
    {
        get; set;
    }

    public long BookingId
    {
        get; set;
    }

    public DateTime? CheckInTime
    {
        get; set;
    }

    public DateTime? CheckOutTime
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

    public virtual Booking Booking { get; set; } = null!;
}
