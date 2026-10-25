using System;
using System.Collections.Generic;

namespace VR.Context;

public partial class VrData
{
    public Guid Id { get; set; }

    public string IdpersonInVacation { get; set; } = null!;

    public string IdpersonReplace { get; set; } = null!;

    public DateTimeOffset FromDate { get; set; }

    public DateTimeOffset? ToDate { get; set; }
}
