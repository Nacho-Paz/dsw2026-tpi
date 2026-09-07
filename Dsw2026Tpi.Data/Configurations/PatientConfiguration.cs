using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Data.Configurations;

internal class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Dni).IsRequired().HasMaxLength(10);
        builder.HasIndex(p => p.Dni).IsUnique();
        builder.HasIndex(p=>p.UserId).IsUnique();
        builder.Property(p => p.FullName).HasMaxLength(150);
        builder.Property(p => p.Phone).HasMaxLength(30);
        builder.Property(x => x.Deleted).IsRequired().HasDefaultValue(false);
    }

}
