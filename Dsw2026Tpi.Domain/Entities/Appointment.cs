using Dsw2026Tpi.Domain.Status;

namespace Dsw2026Tpi.Domain.Entities;

public class Appointment : EntityBase
{
    public Guid AvailabilitySlotId { get; set; }
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public AvailabilitySlot AvailabilitySlot { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.BOOKED;
    public DateTime? CancelledAt { get; set; }
    public DateTime? AttendedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    #region Constructor for EF
#pragma warning disable CS8618
    public Appointment()
    {
    }
#pragma warning restore CS8618
    #endregion
    public Appointment(Guid patientId, Guid availabilitySlotId, string reason)
    {
        PatientId = patientId;
        AvailabilitySlotId = availabilitySlotId;
        Reason = reason;
        Status = AppointmentStatus.BOOKED;
    }
}
