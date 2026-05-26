using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class HazardousWasteCretib
{
    public int HazardousWasteId { get; set; }

    public int CretibId { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Cretib Cretib { get; set; } = null!;

    public virtual HazardousWaste HazardousWaste { get; set; } = null!;
}
