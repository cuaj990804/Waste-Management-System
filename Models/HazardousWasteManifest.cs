using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class HazardousWasteManifest
{
    public int Id { get; set; }

    public string? Folio { get; set; }

    public string WasteName { get; set; } = null!;

    public decimal? Quantity { get; set; }

    public decimal? WeightKg { get; set; }

    public bool? Corrosive { get; set; }

    public bool? Reactive { get; set; }

    public bool? Explosive { get; set; }

    public bool? Toxic { get; set; }

    public bool? Flammable { get; set; }

    public bool? Biological { get; set; }

    public string? GenerationArea { get; set; }

    public string? GenerationManagerName { get; set; }

    public DateOnly? WarehouseEntryDate { get; set; }

    public DateOnly? WarehouseExitDate { get; set; }

    public string? ManifestNumber { get; set; }

    public string? ManifestDeliveredBy { get; set; }

    public string? ManifestReceivedBy { get; set; }

    public string? CollectionTransportName { get; set; }

    public string? CollectionTransportAuthNumber { get; set; }

    public string? FinalDisposalName { get; set; }

    public string? FinalDisposalAuthNumber { get; set; }

    public bool? ManifestSealed { get; set; }

    public string? Comments { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
