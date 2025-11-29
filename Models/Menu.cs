using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Menu
{
    public int MenuId { get; set; }

    public string Title { get; set; } = null!;

    public string? Url { get; set; }

    public string? IconClass { get; set; }

    public int? ParentId { get; set; }

    public int? SortOrder { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Menu> InverseParent { get; set; } = new List<Menu>();

    public virtual Menu? Parent { get; set; }
}
