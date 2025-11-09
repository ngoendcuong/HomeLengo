using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class PropertyFeature
{
    public int PropertyFeatureId { get; set; }

    public int PropertyId { get; set; }

    public int FeatureId { get; set; }

    public string? Value { get; set; }

    public virtual Feature Feature { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;
}
