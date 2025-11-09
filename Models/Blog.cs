using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Blog
{
    public int BlogId { get; set; }

    public int? AuthorId { get; set; }

    public int? CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string? Slug { get; set; }

    public string Content { get; set; } = null!;

    public string? Thumbnail { get; set; }

    public string? Tags { get; set; }

    public int? ViewCount { get; set; }

    public bool? IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual User? Author { get; set; }

    public virtual ICollection<BlogComment> BlogComments { get; set; } = new List<BlogComment>();

    public virtual BlogCategory? Category { get; set; }
}
