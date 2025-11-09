using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int TransactionId { get; set; }

    public string? Provider { get; set; }

    public string? ProviderRef { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? Status { get; set; }

    public virtual Transaction Transaction { get; set; } = null!;
}
