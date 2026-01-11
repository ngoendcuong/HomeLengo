using System;
using System.ComponentModel.DataAnnotations;

namespace HomeLengo.Models;

public partial class FAQ
{
    public int FaqId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Question { get; set; } = null!;

    [Required]
    public string Answer { get; set; } = null!;

    [MaxLength(100)]
    public string? Category { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

