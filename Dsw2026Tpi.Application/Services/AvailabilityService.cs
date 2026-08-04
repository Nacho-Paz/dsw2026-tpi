using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enum;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IPersistence _persistence;
        private readonly ILogger<AvailabilityService> _logger;
        public AvailabilityService(IPersistence persistence, ILogger<AvailabilityService> logger)
        {
            _persistence = persistence;
            _logger = logger;
        }

        public async Task<List<AvailabilityModel.RuleResponse>> CreateAvailabilitiesAsync(AvailabilityModel.Request request)
        {
            _logger.LogInformation("Iniciando el proceso de creación de disponibilidades mensuales para el médico.");

            ValidateRequest(request);
            var doctor = await _persistence.First<Doctor>(d => d.Id == request.DoctorId && d.IsActive);

            if (doctor == null)
            {
                _logger.LogWarning("Proceso interrumpido: El médico solicitado no fue encontrado o se encuentra inactivo en el sistema.");
                throw new EntityNotFoundException(nameof(Doctor));
            }

            var now = DateTime.Now;
            var existingRule = await _persistence.First<AvailabilityRule>(
                r => r.DoctorId == request.DoctorId
                  && r.Month == now.Month
                  && r.Year == now.Year
                  && !r.Deleted);


            if (existingRule != null)
            {
                _logger.LogWarning("Proceso interrumpido: Se detectó que el médico ya cuenta con un cronograma de disponibilidad registrado para el mes.");
                throw new ConflictException("AVAILABILITY_CONFLICT", "El médico ya tiene disponibilidades asignadas para este mes.");
            }

            var rules = await GenerateRulesAndSlots(request, now.Month, now.Year);

            foreach (var rule in rules)
            {
                await _persistence.Add(rule);

            }
            _logger.LogInformation("Las reglas de disponibilidad y sus respectivos turnos fueron generados y guardados exitosamente en la base de datos.");
            return MapToDto(rules);
        }
            

        public async Task<List<AvailabilityModel.RuleResponse>> UpdateAvailabilitiesAsync(AvailabilityModel.Request request)
        {
            _logger.LogInformation("Iniciando el proceso de actualización o reemplazo de disponibilidades mensuales para el médico.");
            ValidateRequest(request);

            var doctor = await _persistence.First<Doctor>(d => d.Id == request.DoctorId && d.IsActive);

            if (doctor == null)
            {
                _logger.LogWarning("Proceso interrumpido: El médico solicitado no fue encontrado o se encuentra inactivo en el sistema.");
                throw new EntityNotFoundException(nameof(Doctor));
            }

            var now = DateTime.Now;

            _logger.LogInformation("Consultando reglas de disponibilidad previas para el mes en curso.");

            var rulesToDelete = await _persistence.GetFiltered<AvailabilityRule>(
                r => r.DoctorId == request.DoctorId
                  && r.Month == now.Month
                  && r.Year == now.Year
                  && !r.Deleted,
                "Slots");

            var preservedSlots = new HashSet<(DateTime Date, TimeSpan Time)>();

            if (rulesToDelete != null && rulesToDelete.Any())
            {
                _logger.LogInformation("Se encontraron reglas previas. Procediendo a dar de baja los horarios libres y conservar los turnos que ya se encontraban reservados.");
                foreach (var rule in rulesToDelete)
                {
                    rule.Deleted = true;
                    if (rule.Slots != null)
                    {
                        foreach (var slot in rule.Slots)
                        {
                            if (slot.Status == SlotStatus.BOOKED)
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
            _logger.LogInformation("Generando las nuevas reglas de disponibilidad integrando los turnos previamente reservados.");

            var newRules = await GenerateRulesAndSlots(request, now.Month, now.Year, preservedSlots);

            foreach (var rule in newRules)
            {
                await _persistence.Add(rule);

            }
            _logger.LogInformation("El proceso de actualización de disponibilidades y reemplazo de horarios finalizó con éxito en la base de datos.");

            return MapToDto(newRules);
        }

        private void ValidateRequest(AvailabilityModel.Request request)
        {
            if (request == null)
            {
                _logger.LogWarning("Validación fallida: El cuerpo de la solicitud para crear disponibilidades se encuentra vacío o nulo.");
                throw new ValidationException("El cuerpo de la solicitud es obligatorio.", ErrorCodes.VALIDATION_ERROR);
            }

            if (request.Days == null || !request.Days.Any())
            {
                _logger.LogWarning("Validación fallida: La solicitud fue rechazada porque no se incluyó ningún día de disponibilidad en la configuración.");
                throw new ValidationException("Debe enviar al menos un día de disponibilidad.", ErrorCodes.VALIDATION_ERROR);
            }
        }


        private async Task<List<AvailabilityRule>> GenerateRulesAndSlots(AvailabilityModel.Request request, int month, int year, HashSet<(DateTime Date, TimeSpan Time)> preservedSlots = null)
        {
            _logger.LogInformation("Iniciando la validación y generación de reglas de disponibilidad y franjas horarias.");

            var groupedDays = request.Days.GroupBy(d => d.Day.Trim().ToUpper());
           
            foreach (var group in groupedDays)
            {
                var sortedRanges = group.Select(d => new
                {
                    StartTime = TimeSpan.Parse(d.StartTime),
                    EndTime = TimeSpan.Parse(d.EndTime)
                }).OrderBy(r => r.StartTime).ToList();


                for (int i = 0; i < sortedRanges.Count - 1; i++)
                {
                    if (sortedRanges[i + 1].StartTime < sortedRanges[i].EndTime)
                    {
                        _logger.LogWarning("Validación fallida: Se detectó un solapamiento en los rangos horarios enviados para un mismo día.");
                        throw new ConflictException("OVERLAPPING_TIMES", $"Se detectó un solapamiento en los horarios enviados para el día {group.Key}.");
                    }
                }
            }

            _logger.LogInformation("Validación de solapamiento de horarios completada sin conflictos. Procediendo a evaluar días y feriados.");

            var rules = new List<AvailabilityRule>();
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var today = DateTime.Now.Date;
           
            var holidays = await LoadHolidaysAsync();

            foreach (var dayRule in request.Days)
            {
                if (!TimeSpan.TryParse(dayRule.StartTime, out var startTime))
                    {

                    _logger.LogWarning("Validación fallida: El horario de inicio proporcionado tiene un formato inválido y no pudo ser procesado.");
                         throw new ConflictException( "INVALID_TIME_FORMAT", $"El horario de inicio del día {dayRule.Day} tiene un formato inválido.");
                    }

                if (!TimeSpan.TryParse(dayRule.EndTime, out var endTime))
                    {
                    _logger.LogWarning("Validación fallida: El horario de fin proporcionado tiene un formato inválido y no pudo ser procesado.");
                    throw new ConflictException("INVALID_TIME_FORMAT", $"El horario de fin del día {dayRule.Day} tiene un formato inválido.");
                    }

                if (startTime >= endTime)
                    {
                    _logger.LogWarning("Validación fallida: Se ingresó un rango inválido donde el horario de inicio es posterior o igual al horario de cierre.");
                    throw new ConflictException( "INVALID_TIME", $"El horario de inicio debe ser menor al de salida para el día {dayRule.Day}");
                    }

                DayOfWeek targetDayOfWeek = MapDayOfWeek(dayRule.Day);

                var rule = new AvailabilityRule
                {
                    DoctorId = request.DoctorId,
                    Month = month,
                    Year = year,
                    DayOfWeek = targetDayOfWeek, 
                    StartTime = startTime,
                    EndTime = endTime,
                    Slots = new List<AvailabilitySlot>()
                };

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
                                    Status = SlotStatus.AVAILABLE,
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
            _logger.LogInformation("La generación de las reglas y la fragmentación de los turnos en intervalos se completó de manera exitosa.");
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
                    _logger.LogWarning("Validación fallida: El texto ingresado para definir el día de la semana no es reconocido como un día válido.");
                    throw new ConflictException("INVALID_DAY", "El día ingresado no es válido.");
            }
        }

        private async Task<HashSet<DateTime>> LoadHolidaysAsync()
        {
            _logger.LogInformation("Iniciando la lectura y carga del archivo local de feriados del sistema.");

            var holidays = new HashSet<DateTime>();
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "feriados.json");

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Operación fallida: No se pudo localizar el archivo físico de configuración de feriados en el directorio del servidor.");
                throw new ValidationException("No se encontró el archivo de feriados.",ErrorCodes.VALIDATION_ERROR);
            }
            
            
            var json = await File.ReadAllTextAsync(filePath);
            var loadedHolidays = JsonSerializer.Deserialize<List<DateTime>>(json);

                if (loadedHolidays != null)
                {
                    foreach (var date in loadedHolidays)
                    {
                        holidays.Add(date.Date);
                    }
                }
            _logger.LogInformation("El archivo de feriados fue leído, decodificado y cargado exitosamente en memoria.");
            return holidays;
        }

        private List<AvailabilityModel.RuleResponse> MapToDto(List<Domain.Entities.AvailabilityRule> rules)
        {
            _logger.LogInformation("Iniciando la transformación de las reglas de disponibilidad al formato de respuesta del sistema.");

            return rules.Select(r => new AvailabilityModel.RuleResponse(
                r.Id,
                r.DoctorId,
                r.Month,
                r.Year,
                r.DayOfWeek.ToString(),
                r.StartTime.ToString(@"hh\:mm"),
                r.EndTime.ToString(@"hh\:mm"),
                r.Slots?.Select(s => new AvailabilityModel.SlotResponse(
                    s.Id,
                    s.SlotDate.ToString("yyyy-MM-dd"),
                    s.StartTime.ToString(@"hh\:mm"),
                    s.EndTime.ToString(@"hh\:mm"),
                    s.Status.ToString()
                )).ToList() ?? new List<AvailabilityModel.SlotResponse>()
            )).ToList();
        }

    }
}
