using System.ComponentModel.DataAnnotations;

namespace Dsw2026Tpi.Application.Dtos
{
    public class SpecialityQueryFilter
    {
        public int PageSize {  get; set; }
        public int PageIndex { get; set; }
        [StringLength(100, MinimumLength = 3, ErrorMessage = "The name must be between 3 and 100 characters")]
        public String? name { get; set; }

    }
}
