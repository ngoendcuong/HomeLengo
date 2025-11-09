using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Tag
{
    public int TagId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<PropertyTag> PropertyTags { get; set; } = new List<PropertyTag>();
}
