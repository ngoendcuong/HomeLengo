using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Feature
{
    public int FeatureId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<PropertyFeature> PropertyFeatures { get; set; } = new List<PropertyFeature>();
}
