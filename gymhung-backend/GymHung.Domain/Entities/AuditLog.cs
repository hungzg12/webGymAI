using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class AuditLog
{
    public long AuditLogId
    {
        get; set;
    }

    public long? UserId
    {
        get; set;
    }

    public string Action { get; set; } = null!;

    public string? EntityName
    {
        get; set;
    }

    public long? EntityId
    {
        get; set;
    }

    public string? OldData
    {
        get; set;
    }

    public string? NewData
    {
        get; set;
    }

    public string? IPAddress
    {
        get; set;
    }

    public string? UserAgent
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual User? User
    {
        get; set;
    }
}
