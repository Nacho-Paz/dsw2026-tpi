using System;
using System.Collections.Generic;
using System.Text;
using static Dsw2026Tpi.Application.Dtos.AppointmentModel;

namespace Dsw2026Tpi.Application.Dtos
{
    public record AppointmentModel
    {
        public record PatientDni(long dni);
        public record Request (Guid DoctorId,Guid AvailabilityId, Guid PatientId,string Reason);
        public record Response(string Message);
    }
}
