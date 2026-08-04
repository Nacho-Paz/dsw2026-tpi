namespace Dsw2026Tpi.Domain.Entities;

public class Patient : EntityBase
{
    public string Dni { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool Deleted { get; set; } = false;
    public DateTime CreatedAtPatient { get; init; } = DateTime.UtcNow;
    public ICollection<Appointment> Appointments { get; set; } = [];
    public Guid UserId { get; private set; }

    #region Constructor for EF
#pragma warning disable CS8618
    private Patient()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Patient(Guid userId, string dni)
    {
        UserId = userId;
        Dni = dni;
        CreatedAt = DateTime.UtcNow;
    }
}
