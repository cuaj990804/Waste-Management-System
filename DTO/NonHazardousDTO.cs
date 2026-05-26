namespace SGA.DTO
{
    public class NonHazardousDTO
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

        public DateOnly? DateIntoWarehouse { get; set; }

        public string? StorageTypeKey { get; set; }

        public string? StorageType { get; set; }
        public string? Comments { get; set; }

    }
}
