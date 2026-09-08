using Dsw2026Tpi.Data.Configurations;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Data;

public class Dsw2026TpiDbContext : DbContext
{
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Specialty> Specialities { get; set; }
    public DbSet<Patient> Patients { get; set; } 
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AvailabilityRule> AvailabilityRules { get; set; }
    public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }



    public Dsw2026TpiDbContext(DbContextOptions<Dsw2026TpiDbContext> options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new AppointmentConfiguration());
        modelBuilder.ApplyConfiguration(new AvailabilityRuleConfiguration());
        modelBuilder.ApplyConfiguration(new AvailabilitySlotConfiguration());
        modelBuilder.ApplyConfiguration(new DoctorConfiguration());
        modelBuilder.ApplyConfiguration(new SpecialityConfiguration());
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
    }
}