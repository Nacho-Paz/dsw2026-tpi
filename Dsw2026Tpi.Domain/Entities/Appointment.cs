using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    internal class Appointment : EntityBase
    {
        public Guid AvailabilitySlotId { get; set; }
        public Guid PatientId { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? AttendedAt { get; set; }
        public byte[] RowVersion { get; set; }
        public AvailabilitySlot AvailabilitySlot { get; set; }
    }
}
