using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Message
{
    public int MessageId { get; set; }

    public int? FromUserId { get; set; }

    public int? ToUserId { get; set; }

    public int? PropertyId { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual User? FromUser { get; set; }

    public virtual Property? Property { get; set; }

    public virtual User? ToUser { get; set; }
}
