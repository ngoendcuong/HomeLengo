using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Property
{
    public int PropertyId { get; set; }

    public int? AgentId { get; set; }

    public string Title { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? Currency { get; set; }

    public decimal? Area { get; set; }

    public int? Bedrooms { get; set; }

    public int? Bathrooms { get; set; }

    public decimal? LotSize { get; set; }

    public int PropertyTypeId { get; set; }

    public int StatusId { get; set; }

    public int? CityId { get; set; }

    public int? DistrictId { get; set; }

    public int? NeighborhoodId { get; set; }

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public int? Views { get; set; }

    public bool? IsFeatured { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Agent? Agent { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual City? City { get; set; }

    public virtual District? District { get; set; }

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Inquiry> Inquiries { get; set; } = new List<Inquiry>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual Neighborhood? Neighborhood { get; set; }

    public virtual ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();

    public virtual ICollection<PropertyFeature> PropertyFeatures { get; set; } = new List<PropertyFeature>();

    public virtual ICollection<PropertyFloorPlan> PropertyFloorPlans { get; set; } = new List<PropertyFloorPlan>();

    public virtual ICollection<PropertyPhoto> PropertyPhotos { get; set; } = new List<PropertyPhoto>();

    public virtual ICollection<PropertyTag> PropertyTags { get; set; } = new List<PropertyTag>();

    public virtual PropertyType PropertyType { get; set; } = null!;

    public virtual ICollection<PropertyVideo> PropertyVideos { get; set; } = new List<PropertyVideo>();

    public virtual ICollection<PropertyVisit> PropertyVisits { get; set; } = new List<PropertyVisit>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual PropertyStatus Status { get; set; } = null!;
}
