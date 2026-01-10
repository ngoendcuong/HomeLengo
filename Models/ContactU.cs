using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class ContactU
{
    public int ContactId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Information { get; set; }

    public string Message { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }
}
