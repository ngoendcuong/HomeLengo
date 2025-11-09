using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class District
{
    public int DistrictId { get; set; }

    public int CityId { get; set; }

    public string Name { get; set; } = null!;

    public virtual City City { get; set; } = null!;

    public virtual ICollection<Neighborhood> Neighborhoods { get; set; } = new List<Neighborhood>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
