using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public record AvailabilityModel
    {
        public record DayRule(string Day, string StartTime, string EndTime);
        public record Request(Guid DoctorId, List<DayRule> Days);
        public record SlotResponse(
            Guid Id,
            string SlotDate,
            string StartTime,
            string EndTime,
            string Status
        );

        public record RuleResponse(
            Guid Id,
            Guid DoctorId,
            int Month,
            int Year,
            string DayOfWeek,
            string StartTime,
            string EndTime,
            List<SlotResponse> Slots
        );

    }

}

