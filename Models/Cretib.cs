using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class Cretib
{
    public int CretibId { get; set; }

    public string CretibKey { get; set; } = null!;

    public string CretibName { get; set; } = null!;

    public string? CretibDescription { get; set; }

    public virtual ICollection<HazardousWasteCretib> HazardousWasteCretibs { get; set; } = new List<HazardousWasteCretib>();
}
