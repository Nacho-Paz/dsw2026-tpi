using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public record AvailabilityModel
    {
        public record DayRule(string Day, string StartTime, string EndTime);
        public record Request(Guid DoctorId, List<DayRule> Days);
        public record Response(string Message);
        
    }

}

