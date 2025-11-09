using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class BlogCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
}
