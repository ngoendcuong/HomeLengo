using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class PropertyTag
{
    public int PropertyTagId { get; set; }

    public int PropertyId { get; set; }

    public int TagId { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}
