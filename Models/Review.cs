using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int PropertyId { get; set; }

    public int? UserId { get; set; }

    public byte Rating { get; set; }

    public string? Title { get; set; }

    public string? Body { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsApproved { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual User? User { get; set; }
}
