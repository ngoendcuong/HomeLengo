using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class ServicePlan
{
    public int PlanId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public bool? IsBroker { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ServiceRegister> ServiceRegisters { get; set; } = new List<ServiceRegister>();
}
