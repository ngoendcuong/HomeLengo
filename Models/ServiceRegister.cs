using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class ServiceRegister
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int PlanId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual ServicePlan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
