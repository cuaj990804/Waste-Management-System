using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class NonHazardou
{
    public int NonHazardousId { get; set; }

    public string? WasteKey { get; set; }

    public string? WasteName { get; set; }

    public string? WasteNameGdi { get; set; }

    public string? PartNumber { get; set; }

    public string? Program { get; set; }

    public string? WasteQuantity { get; set; }

    public double? WasteWeight { get; set; }

    public string? AreaKey { get; set; }

    public string? AreaGdi { get; set; }

    public DateTime? DateIntoWarehouse { get; set; }

    public string? StorageTypeKey { get; set; }

    public string? StorageType { get; set; }

    public DateTime? DateOutoWarehouse { get; set; }

    public string? ManifestNumber { get; set; }

    public string? WasteDestination { get; set; }

    public string? ReturnToClient { get; set; }

    public string? WasteGeneratorNumber { get; set; }

    public string? CollectorName { get; set; }

    public string? CollectionAuthorizationNumber { get; set; }

    public string? CollectionCenterName { get; set; }

    public string? CollectionCenterAuthorizationNumber { get; set; }

    public string? ReuseCompanyName { get; set; }

    public string? ReuseCompanyAuthorizationNumber { get; set; }

    public string? FinalDisposalCompanyName { get; set; }

    public string? FinalDisposalAuthorizationNumber { get; set; }

    public string? SealedManifests { get; set; }

    public string? Comments { get; set; }
}
