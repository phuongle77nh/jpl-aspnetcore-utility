
// See https://aka.ms/new-console-template for more information

public class HolidayType
{
    public string TenantId { get; set; }

    /// <summary>
    /// Name of leave type.
    /// </summary>
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public HolidayType()
    {
    }
}
