using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Dsw2026Tpi.Application.Dtos
{

    public class SpecialityCreateModel
    {
        [Required(ErrorMessage = "The Name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "The Name must be between 3 and 100 characters")]
        public String Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "The Description is required")]
        [StringLength(100, MinimumLength = 10, ErrorMessage = "The Description must be between 10 and 100 characters")]
        public String Description { get; set; } = string.Empty;
    }
}

//TODO: BORRAR VALIDACIONES 