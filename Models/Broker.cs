using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Broker
{
    public int BrokerId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Avatar { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }
}
