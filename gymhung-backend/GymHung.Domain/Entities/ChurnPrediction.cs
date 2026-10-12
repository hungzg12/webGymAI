using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class ChurnPrediction
{
    public long ChurnPredictionId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public DateTime PredictionDate
    {
        get; set;
    }

    public decimal? RiskScore
    {
        get; set;
    }

    public string? RiskLevel
    {
        get; set;
    }

    public string? Factors
    {
        get; set;
    }

    public string? RecommendedActions
    {
        get; set;
    }

    public string? ModelVersion
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual Member Member { get; set; } = null!;
}
