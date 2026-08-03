using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IPersistence _persistence;
        public AvailabilityService(IPersistence persistence)
        {
            _persistence = persistence;
        }

<<<<<<< HEAD

        public async Task<List<AvailabilityRule>> CreateAvailabilitiesAsync(AvailabilityModel.Request request)
        {

            // existe el medico en db?
=======
        public async Task<List<AvailabilityRule>> CreateAvailabilitiesAsync(AvailabilityModel.Request request)
        {
            var doctor = await _persistence.First<Doctor>(d => d.Id == request.DoctorId && d.IsActive);
            
            if (doctor == null) 
            { 
                throw new EntityNotFoundException(nameof(Doctor)); 
            }
>>>>>>> modulo/disponibilidad

            var now = DateTime.Now;
            var existingRule = await _persistence.First<AvailabilityRule>(
                r => r.DoctorId == request.DoctorId
                  && r.Month == now.Month
                  && r.Year == now.Year
                  && !r.Deleted);
            

            if (existingRule != null)
            {
                throw new ConflictException("AVAILABILITY_CONFLICT", "El médico ya tiene disponibilidades asignadas para este mes.");
            }

<<<<<<< HEAD
            /*
            if ()
                //ese dia no es feriado
            { 
                //se puede agregar disponibilidad
            
            }*/


            var rules = GenerateRulesAndSlots(request, now.Month, now.Year);
=======
            var rules = await GenerateRulesAndSlots(request, now.Month, now.Year);
>>>>>>> modulo/disponibilidad

            foreach (var rule in rules)
            {
                await _persistence.Add(rule);
<<<<<<< HEAD
            }

            foreach (var rule in rules)
            {
                await _persistence.Add(rule);
            }

            return rules;
=======
            } return rules;
>>>>>>> modulo/disponibilidad
        }

        public async Task<List<AvailabilityRule>> UpdateAvailabilitiesAsync(AvailabilityModel.Request request)
        {
            var now = DateTime.Now;

            var rulesToDelete = await _persistence.GetFiltered<AvailabilityRule>(
                r => r.DoctorId == request.DoctorId
                  && r.Month == now.Month
                  && r.Year == now.Year
                  && !r.Deleted,
                "Slots");

<<<<<<< HEAD

            // existe el medico en db?

            // chequear si el slot a borrar tiene una cita y booked, arrojar excepcion 
=======
            var preservedSlots = new HashSet<(DateTime Date, TimeSpan Time)>();
>>>>>>> modulo/disponibilidad

            if (rulesToDelete != null && rulesToDelete.Any())
            {
                foreach (var rule in rulesToDelete)
                {
                    rule.Deleted = true;
                    if (rule.Slots != null)
                    {
                        foreach (var slot in rule.Slots)
                        {
                            if (slot.Status == "BOOKED")
                            {
                                preservedSlots.Add((slot.SlotDate.Date, slot.StartTime));
                            }
                            else
                            {
                                slot.Deleted = true;
                            }
                        }
                    }

                    await _persistence.Update(rule);
                }
            }

            var newRules = await GenerateRulesAndSlots(request, now.Month, now.Year, preservedSlots);

            foreach (var rule in newRules)
            {
                await _persistence.Add(rule);
<<<<<<< HEAD
            }

            foreach (var rule in newRules)
            {
                await _persistence.Add(rule);
            }

            return newRules;
=======
            } return newRules;
>>>>>>> modulo/disponibilidad
        }

        private async Task<List<AvailabilityRule>> GenerateRulesAndSlots(AvailabilityModel.Request request, int month, int year, HashSet<(DateTime Date, TimeSpan Time)> preservedSlots = null)
        {
            var groupedDays = request.Days.GroupBy(d => d.Day.Trim().ToUpper());
<<<<<<< HEAD

=======
>>>>>>> modulo/disponibilidad
            foreach (var group in groupedDays)
            {
                var sortedRanges = group.Select(d => new
                {
                    StartTime = TimeSpan.Parse(d.StartTime),
                    EndTime = TimeSpan.Parse(d.EndTime)
                }).OrderBy(r => r.StartTime).ToList();
<<<<<<< HEAD

=======
>>>>>>> modulo/disponibilidad
                for (int i = 0; i < sortedRanges.Count - 1; i++)
                {
                    if (sortedRanges[i + 1].StartTime < sortedRanges[i].EndTime)
                    {
<<<<<<< HEAD
                        throw new ConflictException("OVERLAPPING_TIMES", $"Se detectó un solapamiento en los horarios enviados para el día {group.Key}.");
=======
                        throw new ConflictException("OVERLAPPING_TIMES", $"Se detectó un solapamiento en los horarios enviados para el día { group.Key }.");
>>>>>>> modulo/disponibilidad
                    }
                }
            }

            var rules = new List<AvailabilityRule>();
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var today = DateTime.Now.Date;
           
            var holidays = await LoadHolidaysAsync();

            foreach (var dayRule in request.Days)
            {
                var startTime = TimeSpan.Parse(dayRule.StartTime);
                var endTime = TimeSpan.Parse(dayRule.EndTime);

                if (startTime >= endTime)
                {
                    throw new ConflictException("INVALID_TIME", $"El horario de inicio debe ser menor al de salida para el día {dayRule.Day}");
                }

                var rule = new AvailabilityRule
                {
                    DoctorId = request.DoctorId,
                    Month = month,
                    Year = year,
                    DayOfWeek = dayRule.Day.ToUpper(),
                    StartTime = startTime,
                    EndTime = endTime,
                    Slots = new List<AvailabilitySlot>()
                };

                DayOfWeek targetDayOfWeek = MapDayOfWeek(dayRule.Day);

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var currentDate = new DateTime(year, month, day);

                    if (currentDate.DayOfWeek == targetDayOfWeek && currentDate >= today && !holidays.Contains(currentDate.Date))
                    {
                        var currentSlotStart = startTime;
                        TimeSpan duracionTurno = TimeSpan.FromMinutes(30);

                        while (currentSlotStart + duracionTurno <= endTime)
                        {
                            if (preservedSlots == null || !preservedSlots.Contains((currentDate.Date, currentSlotStart)))

                            {
                                rule.Slots.Add(new AvailabilitySlot
                                {
                                    SlotDate = currentDate,
                                    StartTime = currentSlotStart,
                                    EndTime = currentSlotStart + duracionTurno,
                                    Status = "AVAILABLE",
                                    Deleted = false,
                                    DoctorId = request.DoctorId
                                });
                            }
                              
                            currentSlotStart = currentSlotStart + duracionTurno;
                        }
                    }
                }

                if (rule.Slots.Any())
                {
                    rules.Add(rule);
                }
            }

            return rules;
        }

        private DayOfWeek MapDayOfWeek(string day)
        {
            string diaLimpio = day.Trim().ToUpper();

            switch (diaLimpio)
            {
                case "DOMINGO":
                    return DayOfWeek.Sunday;
                case "LUNES":
                    return DayOfWeek.Monday;
                case "MARTES":
                    return DayOfWeek.Tuesday;
                case "MIERCOLES":
                case "MIÉRCOLES":
                    return DayOfWeek.Wednesday;
                case "JUEVES":
                    return DayOfWeek.Thursday;
                case "VIERNES":
                    return DayOfWeek.Friday;
                case "SABADO":
                case "SÁBADO":
                    return DayOfWeek.Saturday;
                default:
                    throw new ConflictException("INVALID_DAY", "El día ingresado no es válido.");
            }
        }

        private async Task<HashSet<DateTime>> LoadHolidaysAsync()
        {
            var holidays = new HashSet<DateTime>();
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "feriados.json");

            if (!File.Exists(filePath))
            {
                return holidays;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var loadedHolidays = JsonSerializer.Deserialize<List<DateTime>>(json);

                if (loadedHolidays != null)
                {
                    foreach (var date in loadedHolidays)
                    {
                        holidays.Add(date.Date);
                    }
                }
            }
            
            catch (Exception)
            {
                throw;
            }

            return holidays;
        }


    }
}
