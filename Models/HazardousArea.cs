using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class HazardousArea
{
    public int HazardousAreaId { get; set; }

    public string? AreaKey { get; set; }

    public string AreaName { get; set; } = null!;

    public string? AreaDescription { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
