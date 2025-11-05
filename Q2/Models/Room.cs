using System;
using System.Collections.Generic;

namespace Q2.Models;

public partial class Room
{
    public string Title { get; set; } = null!;

    public byte? Square { get; set; }

    public byte? Floor { get; set; }

    public string? Description { get; set; }

    public string? Comment { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
