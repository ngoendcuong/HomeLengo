using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class BlogComment
{
    public int CommentId { get; set; }

    public int BlogId { get; set; }

    public int? UserId { get; set; }

    public string CommentText { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public bool? IsApproved { get; set; }

    public virtual Blog Blog { get; set; } = null!;

    public virtual User? User { get; set; }
}
