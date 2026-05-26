using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class HazardousWaste
{
    public int HazardousWasteId { get; set; }

    public string WasteKey { get; set; } = null!;

    public string WasteName { get; set; } = null!;

    public string? WasteDescription { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<HazardousWasteCretib> HazardousWasteCretibs { get; set; } = new List<HazardousWasteCretib>();
}
