using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Patient : EntityBase
    {
        public Guid Id { get; set; }
        public string Dni { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; }
        public bool Deleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Appointment> Appointments { get; set; }
    }
}
