using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class Member
{
    public long MemberId
    {
        get; set;
    }

    public long UserId
    {
        get; set;
    }

    public decimal? Height
    {
        get; set;
    }

    public decimal? Weight
    {
        get; set;
    }

    public string? Goal
    {
        get; set;
    }

    public string? FitnessLevel
    {
        get; set;
    }

    public string? Address
    {
        get; set;
    }

    public DateOnly JoinDate
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public string? Notes
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

    public virtual ICollection<AIConversation> AIConversations { get; set; } = new List<AIConversation>();

    public virtual ICollection<AIRecommendation> AIRecommendations { get; set; } = new List<AIRecommendation>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    public virtual ICollection<ChurnPrediction> ChurnPredictions { get; set; } = new List<ChurnPrediction>();

    public virtual ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();

    public virtual ICollection<MemberPackage> MemberPackages { get; set; } = new List<MemberPackage>();

    public virtual ICollection<MemberTrainer> MemberTrainers { get; set; } = new List<MemberTrainer>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Progress> Progresses { get; set; } = new List<Progress>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();

    public virtual ICollection<WorkoutResult> WorkoutResults { get; set; } = new List<WorkoutResult>();
}
