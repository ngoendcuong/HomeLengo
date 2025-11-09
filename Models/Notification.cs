using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? UserId { get; set; }

    public string? Title { get; set; }

    public string? Body { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
