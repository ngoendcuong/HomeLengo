using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class ServicePlan
{
    public int PlanId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? MaxListings { get; set; }

    public virtual ICollection<ServicePlanFeature> ServicePlanFeatures { get; set; } = new List<ServicePlanFeature>();

    public virtual ICollection<ServiceRegister> ServiceRegisters { get; set; } = new List<ServiceRegister>();
}
