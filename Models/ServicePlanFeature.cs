using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class ServicePlanFeature
{
    public int FeatureId { get; set; }

    public int PlanId { get; set; }

    public string FeatureText { get; set; } = null!;

    public bool IsIncluded { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ServicePlan Plan { get; set; } = null!;
}
