using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? Avatar { get; set; }

    public virtual ICollection<AdminAudit> AdminAudits { get; set; } = new List<AdminAudit>();

    public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();

    public virtual ICollection<BlogComment> BlogComments { get; set; } = new List<BlogComment>();

    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();

    public virtual ICollection<Message> MessageFromUsers { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageToUsers { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
