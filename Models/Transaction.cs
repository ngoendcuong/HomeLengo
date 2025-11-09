using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int? BookingId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public string? TransactionType { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User User { get; set; } = null!;
}
