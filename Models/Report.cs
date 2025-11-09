using System;
using System.Collections.Generic;

namespace HomeLengo.Models;

public partial class Report
{
    public int ReportId { get; set; }

    public string? ReportType { get; set; }

    public string? Payload { get; set; }

    public DateTime? CreatedAt { get; set; }
}
