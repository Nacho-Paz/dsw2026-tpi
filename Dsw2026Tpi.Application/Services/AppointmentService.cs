using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enum;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Status;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IPersistence _persistence;
        private readonly ILogger<AppointmentService> _logger;
        public AppointmentService(IPersistence persistence, ILogger<AppointmentService> logger)
        {
            _persistence = persistence;
            _logger = logger;
        }
        public async Task<AppointmentModel.Response> CreateAppointmentAsync(AppointmentModel.Request request)
        {
            string dniString = request.Patient.Dni.ToString();
            if (dniString.Length < 7 || dniString.Length > 10) //TODO: Dni entre 7 y 8
            {
                throw new ValidationException("El DNI debe tener entre 7 y 10 dígitos.", "INVALID_DNI");
            }
            _logger.LogInformation("Iniciando solicitud de reserva de turno para el médico {DoctorId} y el slot {SlotId}.",
                request.DoctorId, request.AvailabilitySlotId);

            ValidateRequest(request);
            //string dniString = request.Patient.Dni.ToString();

            var doctor = await _persistence.First<Doctor>(d => d.Id == request.DoctorId);

            if (doctor == null)
            {
                _logger.LogWarning("Médico con ID {DoctorId} no encontrado al intentar crear un turno.", request.DoctorId);
                throw new EntityNotFoundException(nameof(Doctor));
            }

            var slot = await _persistence.First<AvailabilitySlot>(
                s => s.Id == request.AvailabilitySlotId && s.AvailabilityRule.DoctorId == request.DoctorId,
                "AvailabilityRule"
            );

            if (slot == null)
            {
                _logger.LogWarning("El slot {SlotId} no existe o no corresponde al médico {DoctorId}.", request.AvailabilitySlotId, request.DoctorId);
                throw new ConflictException("SLOT_NOT_FOUND", "El horario solicitado no existe o no corresponde a este médico.");
            }

            if (slot.Status != SlotStatus.AVAILABLE)
            {
                _logger.LogWarning("Intento de reserva fallido: El slot {SlotId} no está disponible (Estado actual: {SlotStatus}).", slot.Id, slot.Status);
                throw new ConflictException("SLOT_UNAVAILABLE", "El turno ya no se encuentra disponible.");
            }

            if (slot.SlotDate.Add(slot.StartTime) < DateTime.Now)
            {
                _logger.LogWarning("Intento de reserva fallido: El slot {SlotId} corresponde a una fecha/hora pasada.", slot.Id);
                throw new ConflictException("INVALID_DATE", "No se pueden reservar turnos pasados.");
            }

            var patient = await _persistence.First<Patient>(p => p.Dni == dniString);
            if (patient == null)

                _logger.LogWarning("Paciente con DNI {PatientDni} no encontrado al intentar reservar el slot {SlotId}.", dniString, slot.Id);
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
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Conflicto de concurrencia: El slot {SlotId} fue reservado por otra transacción simultáneamente.", slot.Id);
                throw new ConflictException("APPOINTMENT_CONFLICT", "El turno ya esta reservado");
            }

            _logger.LogInformation("Turno creado exitosamente para el paciente {PatientId} en el slot {SlotId}.", patient.Id, slot.Id);

            return new AppointmentModel.Response(
                appointment.Id,
                appointment.AvailabilitySlotId,
                appointment.PatientId,
                appointment.Reason,
                appointment.Status.ToString(),
                DateTime.Now
            );
        }

        private void ValidateRequest(AppointmentModel.Request request)
        {
            if (request == null)
            {
                _logger.LogWarning("Validación fallida: El cuerpo de la solicitud de reserva es nulo.");
                throw new ValidationException("El cuerpo de la solicitud es obligatorio.", ErrorCodes.VALIDATION_ERROR);
            }

            if (request.Patient == null)
            {
                _logger.LogWarning("Validación fallida: El objeto de datos del paciente es nulo.");
                throw new ValidationException("El paciente es obligatorio.", ErrorCodes.VALIDATION_ERROR);
            }


            if (request.AvailabilitySlotId == Guid.Empty)
            {
                _logger.LogWarning("Validación fallida: El AvailabilitySlotId proporcionado está vacío (Guid.Empty).");
                throw new ValidationException("El availabilitySlotId es obligatorio.", ErrorCodes.VALIDATION_ERROR);
            }

            if (request.Patient.Dni == 0)
            {
                _logger.LogWarning("Validación fallida: El DNI del paciente fue enviado con valor 0.");
                throw new ValidationException("El DNI es obligatorio.", ErrorCodes.VALIDATION_ERROR);
            }

            var dni = request.Patient.Dni.ToString();

            if (dni.Length < 7 || dni.Length > 10)
            {
                _logger.LogWarning("Validación fallida: El DNI tiene una longitud inválida ");
                throw new ValidationException("El DNI debe tener entre 7 y 10 dígitos.", ErrorCodes.VALIDATION_ERROR);
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                _logger.LogWarning("Validación fallida: El motivo de la cita está vacío o es nulo.");
                throw new ValidationException("El motivo es obligatorio.", ErrorCodes.VALIDATION_ERROR);
            }


            if (request.Reason.Trim().Length < 5)
            {
                _logger.LogWarning("Validación fallida: El motivo ingresado es demasiado corto).");
                throw new ValidationException("El motivo debe tener al menos 5 caracteres.", ErrorCodes.VALIDATION_ERROR);
            }

        }

        public async Task<object> GetActiveAppointmentsByPatientAsync(long dni)
        {
            _logger.LogInformation("Consultando turnos activos para el paciente con DNI: {PatientDni}", dni);

            var activeAppointments = await _persistence.GetFiltered<Appointment>(
                a => a.Patient.Dni == dni.ToString() && a.Status == AppointmentStatus.BOOKED,
                "AvailabilitySlot.AvailabilityRule.Doctor,Patient");

            var patient = await _persistence.First<Patient>(p => p.Dni == dni.ToString());

            if (patient == null)
            {
                _logger.LogWarning("Consulta de turnos fallida: No se encontró ningún paciente registrado con el DNI {PatientDni}.", dni);
                throw new EntityNotFoundException(nameof(Patient));
            }

            if (activeAppointments == null || !activeAppointments.Any())
            {
                _logger.LogInformation("El paciente con DNI {PatientDni} no tiene turnos activos en este momento.", dni);
                return new object[] { };
            }

            _logger.LogInformation("Consulta finalizada exitosamente: Se encontraron {AppointmentCount} turnos activos para el DNI {PatientDni}.", activeAppointments.Count(), dni);

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
            _logger.LogInformation("Iniciando solicitud de cancelación");

            var appointment = await _persistence.First<Appointment>(
                a => a.Id == appointmentId,
                "AvailabilitySlot"
            );

            if (appointment == null)
            {
                _logger.LogWarning("Cancelación fallida: No se encontró ningún turno registrado con el ID {AppointmentId}.", appointmentId);
                throw new ConflictException("APPOINTMENT_NOT_FOUND", "Turno no encontrado.");
            }

            if (appointment.Status != AppointmentStatus.BOOKED)
            {
                _logger.LogWarning("Cancelación fallida: El turno {AppointmentId} tiene un estado inválido " +
                    "para esta operación (Estado actual: {AppointmentStatus}).", appointmentId, appointment.Status);
                throw new ConflictException("INVALID_STATUS", "Solo se pueden cancelar turnos que estén en estado BOOKED.");
            }
            appointment.Status = AppointmentStatus.CANCELLED;
            appointment.CancelledAt = DateTime.Now;
            appointment.AvailabilitySlot.Status = SlotStatus.AVAILABLE;

            await _persistence.Update(appointment);
            await _persistence.Update(appointment.AvailabilitySlot);

            _logger.LogInformation("Turno {AppointmentId} cancelado exitosamente. El slot asociado ({SlotId}) ha sido marcado como disponible.", appointmentId, appointment.AvailabilitySlotId);
        }

        public async Task<object> GetAppointmentsByDateAsync(string date)
        {
            _logger.LogInformation("Consultando la grilla de turnos para la fecha: {RequestedDate}", date);

            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                _logger.LogWarning("Consulta de turnos fallida: El valor proporcionado no es un formato de fecha válido.");
                throw new ValidationException("Formato de fecha inválido. Use YYYY-MM-DD.", ErrorCodes.VALIDATION_ERROR);
            }

            var appointments = await _persistence.GetFiltered<Appointment>(
                a => a.AvailabilitySlot.SlotDate.Date == parsedDate.Date,
                "AvailabilitySlot.AvailabilityRule.Doctor,Patient"
            );

            appointments ??= new List<Appointment>();
            _logger.LogInformation("Consulta finalizada exitosamente: Se encontraron {AppointmentCount} " +
                "turnos para la fecha {ParsedDate:yyyy-MM-dd}.", appointments.Count(), parsedDate);

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
            _logger.LogInformation("Iniciando búsqueda paginada de turnos");

            DateTime? parsedDate = null;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime tempDate))
            {
                parsedDate = tempDate.Date;
            }
            else
            {
                _logger.LogWarning("El valor de fecha no es válido y no se aplicará en la búsqueda.");
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
                _logger.LogInformation("La búsqueda de turnos no arrojó resultados con los filtros proporcionados.");
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

            _logger.LogInformation("Búsqueda de turnos exitosa");

            return new Pagination<AppointmentModel.SearchItem>(
                pagedResult.PageSize,
                pagedResult.PageIndex,
                pagedResult.Total,
                data);
        }
    }
}