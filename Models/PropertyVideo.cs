using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class PropertyVideo
{
    public int VideoId { get; set; }

    public int PropertyId { get; set; }

    public string VideoUrl { get; set; } = null!;

    public string? VideoType { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? SortOrder { get; set; }

    public bool? IsPrimary { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Property Property { get; set; } = null!;
}

