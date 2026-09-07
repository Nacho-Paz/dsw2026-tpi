namespace Dsw2026Tpi.Domain.Entities;

public class Specialty: EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDeleted { get; private set; }
    public ICollection<Doctor> Doctors { get; private set; } = new List<Doctor>();

    #region Constructor for EF
#pragma warning disable CS8618
    private Specialty() { }
#pragma warning restore CS8618
    #endregion

    public Specialty(string name, string description, Guid? id = null) : base(id)
    {
        Name = name;
        Description = description;
        IsDeleted = false;
    }

    public void Desactivate()
    {
        IsDeleted = true;
    }
}
