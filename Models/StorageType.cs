using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class StorageType
{
    public int StorageId { get; set; }

    public string? StorageKey { get; set; }

    public string? StorageName { get; set; }
}
