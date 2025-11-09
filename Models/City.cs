using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class City
{
    public int CityId { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public virtual ICollection<District> Districts { get; set; } = new List<District>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
