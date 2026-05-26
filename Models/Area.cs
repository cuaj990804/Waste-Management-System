using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class Area
{
    public int AreaId { get; set; }

    public string? AreaKey { get; set; }

    public string? AreaName { get; set; }

    public string? AreaDescription { get; set; }
}
