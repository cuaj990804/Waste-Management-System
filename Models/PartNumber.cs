using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class PartNumber
{
    public int PartNumberId { get; set; }

    public string? PartNumberKey { get; set; }

    public string? PartNumberName { get; set; }

    public string? PartNumberNameGdi { get; set; }

    public string? PartNumber1 { get; set; }

    public string? PartNumberProgram { get; set; }

    public bool IsReturnMaterial { get; set; }
}
