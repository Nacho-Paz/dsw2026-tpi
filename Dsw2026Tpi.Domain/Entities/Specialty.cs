namespace Dsw2026Tpi.Domain.Entities;

public class Specialty: EntityBase
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsDeleted { get; private set; } 

    #region Constructor for EF
#pragma warning disable CS8618
    private Specialty() { }
#pragma warning restore CS8618
    #endregion

    public Specialty(string name, string description, Guid? id = null) : base(id)
    {
        Name = name;
        Description = description;
    }

    public void Desactivate()
    {
        IsDeleted = true;
    }
}
