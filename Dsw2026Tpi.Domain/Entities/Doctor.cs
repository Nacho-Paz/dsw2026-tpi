namespace Dsw2026Tpi.Domain.Entities;

public class Doctor : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public Guid SpecialityId { get; set; }
    public Specialty Speciality { get; private set; } = null!;
    public ICollection<AvailabilityRule> AvailabilityRules { get; private set; } = new List<AvailabilityRule>();

    #region Constructor for EF
#pragma warning disable CS8618
    private Doctor()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Doctor(string name, string licenseNumber, Specialty speciality, Guid? id = null) : base(id)
    {
        Name = name;
        LicenseNumber = licenseNumber;
        Speciality = speciality;
        IsActive = true;
    }
    public void UpdateData(string name, string licenseNumber, Specialty speciality)
    {
        Name = name;
        LicenseNumber = licenseNumber;
        Speciality = speciality;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

}
