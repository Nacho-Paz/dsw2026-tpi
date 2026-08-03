using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

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
            if (request.Patient.Dni.ToString().Length < 7 || request.Patient.Dni.ToString().Length > 10)
            {
                throw new ValidationException("El DNI debe tener entre 7 y 10 dígitos.", "INVALID_DNI");
            }

            if (request.Reason.Length < 5)
            {
                throw new ValidationException("El motivo debe tener al menos 5 caracteres.", "INVALID_REASON");
            }

            var slot = await _persistence.First<AvailabilitySlot>(
                s => s.Id == request.AvailabilitySlotId && s.AvailabilityRule.DoctorId == request.DoctorId,
                "AvailabilityRule"
            );

            if (slot == null)
            {
                throw new ConflictException("SLOT_NOT_FOUND", "El horario solicitado no existe o no corresponde a este médico.");
            }

            if (slot.Status != "AVAILABLE")
            {
                throw new ConflictException("SLOT_UNAVAILABLE", "El turno ya no se encuentra disponible.");
            }

            var slotDateTime = slot.SlotDate.Add(slot.StartTime);
            if (slotDateTime < DateTime.Now)
            {
                throw new ConflictException("INVALID_DATE", "No se pueden reservar turnos pasados.");
            }

            var patient = await _persistence.First<Patient>(p => p.Dni == request.Patient.Dni.ToString());
            if (patient == null)
                throw new ConflictException("PATIENT_NOT_FOUND", "El paciente no existe en el sistema.");

            var appointment = new Appointment
            {
                AvailabilitySlotId = slot.Id,
                PatientId = patient.Id,
                Reason = request.Reason,
                Status = "BOOKED"
            };

            slot.Status = "BOOKED";

            await _persistence.Add(appointment);
            await _persistence.Update(slot);

            return new AppointmentModel.Response(
                appointment.Id,
                appointment.AvailabilitySlotId,
                appointment.PatientId,
                appointment.Reason,
                appointment.Status,
                DateTime.Now
            );
        }

        public async Task<object> GetActiveAppointmentsByPatientAsync(long dni)
        {
            var activeAppointments = await _persistence.GetFiltered<Appointment>(
                a => a.Patient.Dni == dni.ToString() && a.Status == "BOOKED",
                "AvailabilitySlot.AvailabilityRule.Doctor,Patient");

            var patient = await _persistence.First<Patient>(p => p.Dni == dni.ToString());

            if (patient == null)
            {
                throw new EntityNotFoundException("Paciente");
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

            if (appointment.Status != "BOOKED")
            {
                throw new ConflictException("INVALID_STATUS", "Solo se pueden cancelar turnos que estén en estado BOOKED.");
            }
            appointment.Status = "CANCELLED";
            appointment.CancelledAt = DateTime.Now;
            appointment.AvailabilitySlot.Status = "AVAILABLE";

            await _persistence.Update(appointment);
            await _persistence.Update(appointment.AvailabilitySlot);
        }

        public async Task<object> GetAppointmentsByDateAsync(string date)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                throw new ValidationException("Formato de fecha inválido. Use YYYY-MM-DD.", "INVALID_DATE");
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

            var allAppointments = await _persistence.GetFiltered<Appointment>(
                a => (!parsedDate.HasValue || a.AvailabilitySlot.SlotDate.Date == parsedDate.Value) &&
                     (!doctorId.HasValue || a.AvailabilitySlot.AvailabilityRule.DoctorId == doctorId.Value) &&
                     (!dni.HasValue || a.Patient.Dni == dni.Value.ToString()),
                "AvailabilitySlot.AvailabilityRule.Doctor.Specialty,Patient"
            );

            allAppointments ??= new List<Appointment>();
            if (!allAppointments.Any())
            {
                return Pagination<AppointmentModel.SearchItem>.Empty;
            }

            if (specialtyId.HasValue)
            {
                allAppointments = allAppointments.Where(a =>
                    a.AvailabilitySlot?.AvailabilityRule?.Doctor?.Speciality?.Id == specialtyId.Value).ToList();
            }

            int totalRecords = allAppointments.Count();
            var pagedAppointments = allAppointments
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var data = pagedAppointments.Select(a => new AppointmentModel.SearchItem(
                AppointmentsId: a.Id,
                AppointmentsStatus: a.Status,
                Patient: new AppointmentModel.PatientInfo(
                    Dni: long.Parse(a.Patient?.Dni ?? "0"),
                    FullName: a.Patient?.Nombre ?? ""),
                Doctor: new AppointmentModel.DoctorInfo(
                    DoctorId: a.AvailabilitySlot?.AvailabilityRule?.Doctor?.Id ?? Guid.Empty,
                    Name: a.AvailabilitySlot?.AvailabilityRule?.Doctor?.Name ?? "",
                    Specialty: new AppointmentModel.SpecialtyInfo(
                        SpecialtyId: a.AvailabilitySlot?.AvailabilityRule?.Doctor?.Speciality?.Id ?? Guid.Empty,
                        Name: a.AvailabilitySlot?.AvailabilityRule?.Doctor?.Speciality?.Name ?? "")
                                                        )));

            return new Pagination<AppointmentModel.SearchItem>(
                pageSize,
                pageIndex,
                totalRecords,
                data);
        }
    }
}
