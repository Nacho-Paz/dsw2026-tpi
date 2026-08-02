using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Patient : EntityBase
    {
        public Guid Id { get; set; }
        public string Dni { get; set; }
        public string Nombre { get; set; } 
        public string Telefono { get; set; }
        public bool Deleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Appointment> Appointments { get; set; }
    }
}
