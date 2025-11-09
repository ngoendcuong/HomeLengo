using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Agent
{
    public int AgentId { get; set; }

    public int? UserId { get; set; }

    public string? AgencyName { get; set; }

    public string? LicenseNumber { get; set; }

    public string? Bio { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public virtual User? User { get; set; }
}
