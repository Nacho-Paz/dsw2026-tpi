using System;
using System.Collections.Generic;
using System.Text;
using static Dsw2026Tpi.Application.Dtos.AppointmentModel;

namespace Dsw2026Tpi.Application.Dtos
{
    public record AppointmentModel
    {
        public record Request(Guid DoctorId, Guid AvailabilitySlotId, PatientRequest Patient, string Reason);
        public record PatientRequest(long Dni);
    }
}
