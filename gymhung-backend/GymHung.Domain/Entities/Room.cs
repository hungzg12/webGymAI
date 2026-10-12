using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Room
{
    public long RoomId
    {
        get; set;
    }

    public string RoomName { get; set; } = null!;

    public string? Location
    {
        get; set;
    }

    public int? Capacity
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public string? Description
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

    public virtual ICollection<WorkoutSchedule> WorkoutSchedules { get; set; } = new List<WorkoutSchedule>();
}
