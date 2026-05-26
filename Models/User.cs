using System;
using System.Collections.Generic;

namespace SGA.Models;

public partial class User
{
    public int UserId { get; set; }

    public string EmployeeNumber { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Area { get; set; }

    public string? UserRole { get; set; }
}
