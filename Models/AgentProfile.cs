using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace HomeLengo.Models;

public partial class AgentProfile
{
    public int Id { get; set; }

    public int ServiceRegisterId { get; set; }

    public string? DisplayName { get; set; }

    public string? Avatar { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    [NotMapped]
    public IFormFile? AvatarFile { get; set; }

    public virtual ServiceRegister ServiceRegister { get; set; } = null!;
}
