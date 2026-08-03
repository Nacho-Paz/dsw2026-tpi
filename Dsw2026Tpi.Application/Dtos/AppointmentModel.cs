using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public record AppointmentModel
    {
        public record Request(Guid DoctorId, Guid AvailabilitySlotId, PatientRequest Patient, string Reason);
        public record PatientRequest(long Dni);
        public record Response(Guid Id, Guid AvailabilitySlotId, Guid PatientId, string Reason, string Status, DateTime CreatedAt);
        public record SearchItem(Guid AppointmentsId, string AppointmentsStatus, PatientInfo Patient, DoctorInfo Doctor);

        public record PatientInfo(long Dni, string FullName);

        public record DoctorInfo(Guid DoctorId,string Name, SpecialtyInfo Specialty );

        public record SpecialtyInfo(Guid SpecialtyId, string Name);

    }
}
//TODO: REVISAR CON TPI 