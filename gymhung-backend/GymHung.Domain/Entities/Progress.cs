using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Progress
{
    public long ProgressId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public DateOnly RecordDate
    {
        get; set;
    }

    public decimal? BodyFat
    {
        get; set;
    }

    public decimal? Weight
    {
        get; set;
    }

    public decimal? MuscleMass
    {
        get; set;
    }

    public decimal? Chest
    {
        get; set;
    }

    public decimal? Waist
    {
        get; set;
    }

    public decimal? Hip
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

    public virtual Member Member { get; set; } = null!;
}
