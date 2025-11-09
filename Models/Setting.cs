using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Setting
{
    public string SettingKey { get; set; } = null!;

    public string? SettingValue { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
