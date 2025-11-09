using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Neighborhood
{
    public int NeighborhoodId { get; set; }

    public int DistrictId { get; set; }

    public string Name { get; set; } = null!;

    public virtual District District { get; set; } = null!;

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
