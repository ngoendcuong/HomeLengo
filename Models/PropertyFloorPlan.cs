using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class PropertyFloorPlan
{
    public int FloorPlanId { get; set; }

    public int PropertyId { get; set; }

    public string FloorName { get; set; } = null!;

    public string ImagePath { get; set; } = null!;

    public decimal? Area { get; set; }

    public int? Bedrooms { get; set; }

    public int? Bathrooms { get; set; }

    public string? Description { get; set; }

    public int? SortOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Property Property { get; set; } = null!;
}
