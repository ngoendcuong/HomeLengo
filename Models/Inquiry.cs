using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Inquiry
{
    public int InquiryId { get; set; }

    public int PropertyId { get; set; }

    public int? UserId { get; set; }

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public string? Message { get; set; }

    public DateTime? PreferredTime { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual User? User { get; set; }
}
