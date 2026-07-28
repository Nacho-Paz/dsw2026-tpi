using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using System.Linq;
using Dsw2026Tpi.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityService
    {
        private readonly Dsw2026TpiDbContext _context;

        public AvailabilityService(Dsw2026TpiDbContext context)
        {
            _context = context;
        }

        public async Task CreateAvailabilitiesAsync(AvailabilityModel.Request request)
        {
            var now = DateTime.Now;

            var existingRules = await _context.Set<AvailabilityRule>()
                .AnyAsync(r => r.DoctorId == request.DoctorId
                            && r.Month == now.Month
                            && r.Year == now.Year
                            && !r.Deleted);

            if (existingRules)
            {
                throw new ConflictException("El médico ya tiene disponibilidades asignadas para el mes.");
            }

            var rules = GenerateRulesAndSlots(request, now.Month, now.Year);

            await _context.Set<AvailabilityRule>().AddRangeAsync(rules);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAvailabilitiesAsync(AvailabilityModel.Request request)
        {
            var now = DateTime.Now;


            var rulesToDelete = await _context.Set<AvailabilityRule>()
                .Include(r => r.Slots)
                .Where(r => r.DoctorId == request.DoctorId
                         && r.Month == now.Month
                         && r.Year == now.Year
                         && !r.Deleted)
                .ToListAsync();

            foreach (var rule in rulesToDelete)
            {
                rule.Deleted = true;
                foreach (var slot in rule.Slots)
                {
                    slot.Deleted = true;
                }
            }

            var newRules = GenerateRulesAndSlots(request, now.Month, now.Year);
            await _context.Set<AvailabilityRule>().AddRangeAsync(newRules);

            await _context.SaveChangesAsync();
        }

        private List<AvailabilityRule> GenerateRulesAndSlots(AvailabilityModel.Request request, int month, int year)
        {
            var rules = new List<AvailabilityRule>();
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var today = DateTime.Now.Date;

            foreach (var dayRule in request.Days)
            {
                var startTime = TimeSpan.Parse(dayRule.StartTime);
                var endTime = TimeSpan.Parse(dayRule.EndTime);

                if (startTime >= endTime)
                    throw new Exception($"El horario de inicio debe ser menor al de salida para el día {dayRule.Day}");

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

                    if (currentDate.DayOfWeek == targetDayOfWeek && currentDate >= today)
                    {
                        var currentSlotStart = startTime;
                        TimeSpan duracionTurno = TimeSpan.FromMinutes(30);

                        while (currentSlotStart + duracionTurno <= endTime)
                        {
                            rule.Slots.Add(new AvailabilitySlot
                            {
                                SlotDate = currentDate,
                                StartTime = currentSlotStart,
                                EndTime = currentSlotStart + duracionTurno,
                                Status = "AVAILABLE",
                                Deleted = false
                            });

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
                    throw new Exception("El día ingresado no es válido.");
            }
        }
    }
}

