namespace VR_GUI.Models;

public sealed class VrDataItem
{
    public Guid Id { get; set; }

    public string IdpersonInVacation { get; set; } = string.Empty;

    public string IdpersonReplace { get; set; } = string.Empty;

    public DateTimeOffset FromDate { get; set; }

    public DateTimeOffset? ToDate { get; set; }
}
