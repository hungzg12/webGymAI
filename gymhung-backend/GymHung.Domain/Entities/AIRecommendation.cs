using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class AIRecommendation
{
    public long RecommendationId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public string RecommendationType { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? ReferenceData
    {
        get; set;
    }

    public bool IsRead
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual Member Member { get; set; } = null!;
}
