using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class SearchHistory
{
    public int SearchId { get; set; }

    public int? UserId { get; set; }

    public string? QueryText { get; set; }

    public string? Filters { get; set; }

    public int? ResultCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
