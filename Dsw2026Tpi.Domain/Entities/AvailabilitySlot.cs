using Dsw2026Tpi.Domain.Enum;
using Dsw2026Tpi.Domain.Status;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilitySlot : EntityBase
    {
        public Guid AvailabilityRuleId { get; set; }
        public DateTime SlotDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public SlotStatus Status { get; set; } = SlotStatus.AVAILABLE;
        public bool Deleted { get; set; } = false;
        public byte[] RowVersion { get; set; }
        public Guid DoctorId { get; set; }
        public AvailabilityRule AvailabilityRule { get; set; }
        public Appointment Appointment { get; set; }

    }
}
