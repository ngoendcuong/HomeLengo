using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class PropertyVisit
{
    public long VisitId { get; set; }

    public int PropertyId { get; set; }

    public string? VisitorIp { get; set; }

    public int? UserId { get; set; }

    public string? UserAgent { get; set; }

    public DateTime? VisitedAt { get; set; }

    public virtual Property Property { get; set; } = null!;
}
