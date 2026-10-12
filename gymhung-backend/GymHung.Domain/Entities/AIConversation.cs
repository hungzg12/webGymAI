using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class AIConversation
{
    public long ConversationId
    {
        get; set;
    }

    public long MemberId
    {
        get; set;
    }

    public string? ConversationTitle
    {
        get; set;
    }

    public string? ContextData
    {
        get; set;
    }

    public DateTime StartedAt
    {
        get; set;
    }

    public DateTime? UpdatedAt
    {
        get; set;
    }

    public string Status { get; set; } = null!;

    public virtual ICollection<AIConversationMessage> AIConversationMessages { get; set; } = new List<AIConversationMessage>();

    public virtual Member Member { get; set; } = null!;
}
