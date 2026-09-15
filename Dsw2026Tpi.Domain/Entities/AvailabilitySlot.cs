using Dsw2026Tpi.Domain.Enum;
namespace Dsw2026Tpi.Domain.Entities;

public class AvailabilitySlot : EntityBase
{
    public Guid AvailabilityRuleId { get; set; }
    public DateTime SlotDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public SlotStatus Status { get; set; } = SlotStatus.AVAILABLE;
    public bool Deleted { get; set; } = false;
    public byte[] RowVersion { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public AvailabilityRule AvailabilityRule { get; set; } = null!;
    public Appointment? Appointment { get; set; }

    #region Constructor for EF
#pragma warning disable CS8618
    public AvailabilitySlot()
    {
    }
#pragma warning restore CS8618
    #endregion
}
