using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class AdminAudit
{
    public int AuditId { get; set; }

    public int? AdminUserId { get; set; }

    public string? Action { get; set; }

    public string? TargetTable { get; set; }

    public string? TargetId { get; set; }

    public string? Details { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? AdminUser { get; set; }
}
