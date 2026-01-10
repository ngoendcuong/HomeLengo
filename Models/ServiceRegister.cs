using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class ServiceRegister
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int PlanId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AgentProfile> AgentProfiles { get; set; } = new List<AgentProfile>();

    public virtual ServicePlan Plan { get; set; } = null!;
}
