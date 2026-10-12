using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class EmergencyContact
{
    public long ContactId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Relationship
    {
        get; set;
    }

    public string? Notes
    {
        get; set;
    }

    public virtual Member Member { get; set; } = null!;
}
