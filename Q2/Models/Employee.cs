using System;
using System.Collections.Generic;

namespace Q2.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Gender { get; set; }

    public DateOnly? Dob { get; set; }

    public string? Phone { get; set; }

    public string? Idnumber { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
