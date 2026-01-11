using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class UserServicePackage
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int PlanId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ServicePlan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

