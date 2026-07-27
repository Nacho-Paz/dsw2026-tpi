using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Dsw2026Tpi.Application.Dtos
{
    internal class SpecialityQueryFilter
    {
        public int pageSize {  get; set; }
        public int pageIndex { get; set; }
        [StringLength(100, MinimumLength = 3, ErrorMessage = "The name must be between 3 and 100 characters")]
        public String? name { get; set; }

    }
}
