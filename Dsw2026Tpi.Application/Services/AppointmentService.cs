using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enum;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Status;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Application.Services
{
    public class AppointmentService : IAppointmentService 
    {
        private readonly IPersistence _persistence;
        public AppointmentService(IPersistence persistence)
        {
            _persistence = persistence;
        }
        public async Task<AppointmentModel.Response> CreateAppointmentAsync(AppointmentModel.Request request)
        {
            ValidateRequest(request);
            string dniString = request.Patient.Dni.ToString();

            var doctor = await _persistence.First<Doctor>(d => d.Id == request.DoctorId);

            if (doctor == null)
            {
                throw new EntityNotFoundException(nameof(Doctor));
            }

            var slot = await _persistence.First<AvailabilitySlot>(
                s => s.Id == request.AvailabilitySlotId && s.AvailabilityRule.DoctorId == request.DoctorId,
                "AvailabilityRule"
            );

            if (slot == null)
            {
                throw new ConflictException("SLOT_NOT_FOUND", "El horario solicitado no existe o no corresponde a este médico.");
            }

            if (slot.Status != SlotStatus.AVAILABLE)
            {
                throw new ConflictException("SLOT_UNAVAILABLE", "El turno ya no se encuentra disponible.");
            }

            if (slot.SlotDate.Add(slot.StartTime) < DateTime.Now)
            {
                throw new ConflictException("INVALID_DATE", "No se pueden reservar turnos pasados.");
            }

            var patient = await _persistence.First<Patient>(p => p.Dni == dniString);
            if (patient == null)
                throw new ConflictException("PATIENT_NOT_FOUND", "El paciente no existe en el sistema.");

            var appointment = new Appointment
            {
                AvailabilitySlotId = slot.Id,
                PatientId = patient.Id,
                Reason = request.Reason,
                Status = AppointmentStatus.BOOKED
            };

            slot.Status = SlotStatus.BOOKED;

            await _persistence.Add(appointment);
            
            try
            {
                await _persistence.Update(slot);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("APPOINTMENT_CONFLICT", "El turno ya esta reservado");
            }

            return new AppointmentModel.Response(
                appointment.Id,
                appointment.AvailabilitySlotId,
                appointment.PatientId,
                appointment.Reason,
                appointment.Status.ToString(), 
                DateTime.Now
            );
        }

        private static void ValidateRequest(AppointmentModel.Request request)
        {
            if (request == null) throw new ValidationException( 
                "El cuerpo de la solicitud es obligatorio.", ErrorCodes.VALIDATION_ERROR);

            if (request.Patient == null) throw new ValidationException(
                "El paciente es obligatorio.", ErrorCodes.VALIDATION_ERROR);

            if (request.AvailabilitySlotId == Guid.Empty) throw new ValidationException(
                "El availabilitySlotId es obligatorio.", ErrorCodes.VALIDATION_ERROR);

            if (request.Patient.Dni == 0) throw new ValidationException(
                "El DNI es obligatorio.", ErrorCodes.VALIDATION_ERROR);

            var dni = request.Patient.Dni.ToString();

            if (dni.Length < 7 || dni.Length > 10) throw new ValidationException(
                "El DNI debe tener entre 7 y 10 dígitos.", ErrorCodes.VALIDATION_ERROR);

            if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException( 
                "El motivo es obligatorio.", ErrorCodes.VALIDATION_ERROR);

            if (request.Reason.Trim().Length < 5)
                throw new ValidationException(
                    "El motivo debe tener al menos 5 caracteres.", ErrorCodes.VALIDATION_ERROR);
        }

        public async Task<object> GetActiveAppointmentsByPatientAsync(long dni)
        {
            var activeAppointments = await _persistence.GetFiltered<Appointment>(
                a => a.Patient.Dni == dni.ToString() && a.Status == AppointmentStatus.BOOKED,
                "AvailabilitySlot.AvailabilityRule.Doctor,Patient");

            var patient = await _persistence.First<Patient>(p => p.Dni == dni.ToString());

            if (patient == null)
            {
                throw new EntityNotFoundException(nameof(Patient));
            }

            if (activeAppointments == null || !activeAppointments.Any())
            {
                return new object[] { };
            }

            return activeAppointments.Select(a => new
            {
                AppointmentId = a.Id,
                DoctorName = a.AvailabilitySlot.AvailabilityRule.Doctor.Name,
                Date = a.AvailabilitySlot.SlotDate,
                StartTime = a.AvailabilitySlot.StartTime,
                Status = a.Status
            }).ToList();
        }        

        public async Task CancelAppointmentAsync(Guid appointmentId)
        {
            var appointment = await _persistence.First<Appointment>(
                a => a.Id == appointmentId,
                "AvailabilitySlot"
            );

            if (appointment == null)
            {
                throw new ConflictException("APPOINTMENT_NOT_FOUND", "Turno no encontrado.");
            }

            if (appointment.Status != AppointmentStatus.BOOKED)
            {
                throw new ConflictException("INVALID_STATUS", "Solo se pueden cancelar turnos que estén en estado BOOKED.");
            }
            appointment.Status = AppointmentStatus.CANCELLED;
            appointment.CancelledAt = DateTime.Now;
            appointment.AvailabilitySlot.Status = SlotStatus.AVAILABLE;

            await _persistence.Update(appointment);
            await _persistence.Update(appointment.AvailabilitySlot);
        }

        public async Task<object> GetAppointmentsByDateAsync(string date)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                throw new ValidationException("Formato de fecha inválido. Use YYYY-MM-DD.", ErrorCodes.VALIDATION_ERROR);
            }

            var appointments = await _persistence.GetFiltered<Appointment>(
                a => a.AvailabilitySlot.SlotDate.Date == parsedDate.Date,
                "AvailabilitySlot.AvailabilityRule.Doctor,Patient"
            );

            appointments ??= new List<Appointment>();

            return appointments.Select(a => new
            {
                AppointmentId = a.Id,
                Status = a.Status,
                PatientDni = a.Patient.Dni ?? "",
                Time = a.AvailabilitySlot?.StartTime ?? TimeSpan.Zero
            }).ToList();
        }

        public async Task<Pagination<AppointmentModel.SearchItem>> SearchAppointmentsAsync(int pageSize, int pageIndex, Guid? specialtyId, Guid? doctorId, long? dni, string date)
        {
            DateTime? parsedDate = null;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime tempDate))
            {
                parsedDate = tempDate.Date;
            }

            System.Linq.Expressions.Expression<Func<Appointment, bool>> predicate = a =>
            (!parsedDate.HasValue || (a.AvailabilitySlot != null && a.AvailabilitySlot.SlotDate.Date == parsedDate.Value)) &&
            (!doctorId.HasValue || (a.AvailabilitySlot != null && a.AvailabilitySlot.AvailabilityRule != null && a.AvailabilitySlot.AvailabilityRule.DoctorId == doctorId.Value)) &&
            (!dni.HasValue || (a.Patient != null && a.Patient.Dni == dni.Value.ToString())) &&
            (!specialtyId.HasValue || (a.AvailabilitySlot != null &&
                                a.AvailabilitySlot.AvailabilityRule != null &&
                               a.AvailabilitySlot.AvailabilityRule.Doctor != null &&
                               a.AvailabilitySlot.AvailabilityRule.Doctor.Speciality != null &&
                               a.AvailabilitySlot.AvailabilityRule.Doctor.Speciality.Id == specialtyId.Value));

            System.Linq.Expressions.Expression<Func<Appointment, DateTime>> sortOrder = a => a.AvailabilitySlot.SlotDate;

            var pagedResult = await _persistence.Paginate(
                pageSize,
                pageIndex,
                predicate,
                sortOrder,
                "AvailabilitySlot.AvailabilityRule.Doctor.Speciality",
                "Patient"
            );


            if (pagedResult == null || !pagedResult.Data.Any())
            {
                return Pagination<AppointmentModel.SearchItem>.Empty;
            }

            var data = pagedResult.Data.Select(a => new AppointmentModel.SearchItem(
                AppointmentId: a.Id,
                AppointmentStatus: a.Status.ToString(),
                Patient: new AppointmentModel.PatientInfo(
                    Dni: long.Parse(a.Patient?.Dni ?? "0"),
                    FullName: a.Patient?.FullName ?? ""),
                Doctor: new AppointmentModel.DoctorInfo(
                    DoctorId: a.AvailabilitySlot?.AvailabilityRule?.Doctor?.Id ?? Guid.Empty,
                    Name: a.AvailabilitySlot?.AvailabilityRule?.Doctor?.Name ?? "",
                    Specialty: new AppointmentModel.SpecialtyInfo(
                        SpecialtyId: a.AvailabilitySlot?.AvailabilityRule?.Doctor?.Speciality?.Id ?? Guid.Empty,
                        Name: a.AvailabilitySlot?.AvailabilityRule?.Doctor?.Speciality?.Name ?? "")
                )));

            return new Pagination<AppointmentModel.SearchItem>(
                pagedResult.PageSize,
                pagedResult.PageIndex,
                pagedResult.Total,
                data);
        }
    }
}
