using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public class DoctorCreateModel
    {
        [Required(ErrorMessage = "El nombre del médico es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "La matrícula es obligatoria.")]
        public string LicenseNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "Debe asociar una especialidad.")]
        public Guid SpecialtyId { get; set; }
    }
}
