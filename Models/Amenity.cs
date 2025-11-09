using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Amenity
{
    public int AmenityId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();
}
