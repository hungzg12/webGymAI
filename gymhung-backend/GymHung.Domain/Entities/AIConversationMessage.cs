using System;
using System.Collections.Generic;

namespace GymManagement.Api.Data.Entities;

public partial class AIConversationMessage
{
    public long MessageId
    {
        get; set;
    }

    public long ConversationId
    {
        get; set;
    }

    public string SenderType { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? ContextData
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public virtual AIConversation Conversation { get; set; } = null!;
}
