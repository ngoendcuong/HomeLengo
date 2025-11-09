using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class PropertyStatus
{
    public int StatusId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
