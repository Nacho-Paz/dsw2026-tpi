namespace Dsw2026Tpi.Domain.Entities;

public class AvailabilityRule : EntityBase
{
    public Guid DoctorId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool Deleted { get; set; } = false; 
    public Doctor Doctor { get; set; }
    public ICollection<AvailabilitySlot> Slots { get; set; } = new List<AvailabilitySlot>();

    #region Constructor for EF
#pragma warning disable CS8618
    public AvailabilityRule()
    {
    }
#pragma warning restore CS8618
    #endregion

}
