using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Dsw2026Tpi.Application.Dtos;

public record DoctorModel
{
    public record Request(
       //  [property: Required(ErrorMessage = "El nombre es obligatorio.")]
       // [property: StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
       // string Name,

       // [property: Required(ErrorMessage = "La matrícula es obligatoria.")]
       // string LicenseNumber,

       //[property: Required(ErrorMessage = "La especialidad es obligatoria.")]
       //[property: JsonPropertyName("specialtyId")] 
       // Guid SpecialtyId 
       string Name,
       string LicenseNumber,
       Guid specialtyId
    );

    public record Response(Guid Id, string Name, string LicenseNumber, SpecialtyDto? Specialty);

    public record SpecialtyDto(
       // [property: JsonPropertyName("id")] Guid? SpecialtyId,
       // string? Name
       Guid? SpecialtyId,
       string? Name
    );

    public record AvailabilityResponse(Guid Id, string Day, string StartTime, string EndTime);
}

