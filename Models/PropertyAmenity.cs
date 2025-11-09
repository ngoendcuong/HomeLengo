using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class PropertyAmenity
{
    public int PropertyAmenityId { get; set; }

    public int PropertyId { get; set; }

    public int AmenityId { get; set; }

    public int? DistanceMeters { get; set; }

    public virtual Amenity Amenity { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;
}
