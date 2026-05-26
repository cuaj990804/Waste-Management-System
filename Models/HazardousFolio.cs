using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class HazardousFolio
{
    public int HazardousFolioId { get; set; }

    public string? FolioNumber { get; set; }

    public int? HazardousWasteId { get; set; }

    public string WasteName { get; set; } = null!;

    public int WasteQuantity { get; set; }

    public decimal? WasteWeight { get; set; }

    public int? AreaId { get; set; }

    public string AreaName { get; set; } = null!;

    public DateTime DateIntoWarehouse { get; set; }

    public DateTime? DateOutOfWarehouse { get; set; }

    public string? ManifestNumber { get; set; }

    public string? WasteDestination { get; set; }

    public string? WasteGeneratorNumber { get; set; }

    public string? CollectorName { get; set; }

    public string? CollectionAuthorizationNumber { get; set; }

    public string? CollectionCenterName { get; set; }

    public string? CollectionCenterAuthorizationNumber { get; set; }

    public string? FinalDisposalCompanyName { get; set; }

    public string? FinalDisposalAuthorizationNumber { get; set; }

    public string? SealedManifests { get; set; }

    public string? Comments { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
