using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int PropertyId { get; set; }

    public int UserId { get; set; }

    public int? AgentId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Agent? Agent { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual User User { get; set; } = null!;
}
