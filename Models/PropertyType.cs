using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class PropertyType
{
    public int PropertyTypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
