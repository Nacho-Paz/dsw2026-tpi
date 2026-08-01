using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using System;
using System.Collections.Generic;
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

        public async Task CreateAppointmentAsync(AppointmentModel.Request request)
        {
            var slot = await _persistence.First<AvailabilitySlot>(
                s => s.Id == request.AvailabilityId && s.AvailabilityRule.DoctorId == request.DoctorId,
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

            var appointment = new Appointment
            {
                AvailabilitySlotId = slot.Id,
                PatientId = request ??
                Reason = request.Reason,
                Status = "BOOKED"
            };

            slot.Status = "BOOKED";

            await _persistence.Add(appointment);
            await _persistence.Update(slot);
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

        public async Task<object> GetActiveAppointmentsByPatientAsync(long dni)
        {
            var patient = await _persistence.First<??>(p => p.Dni == dni.ToString());
            if (patient == null)
            {
                throw new ConflictException("PATIENT_NOT_FOUND", "Paciente no encontrado.");
            }

            var activeAppointments = await _persistence.GetFiltered<Appointment>(
                a => a.PatientId == patient.Id && a.Status == "BOOKED",
                "AvailabilitySlot.AvailabilityRule.Doctor"
            );

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
    }
}
