using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class PropertyPhoto
{
    public int PhotoId { get; set; }

    public int PropertyId { get; set; }

    public string FilePath { get; set; } = null!;

    public string? AltText { get; set; }

    public bool? IsPrimary { get; set; }

    public int? SortOrder { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual Property Property { get; set; } = null!;
}
