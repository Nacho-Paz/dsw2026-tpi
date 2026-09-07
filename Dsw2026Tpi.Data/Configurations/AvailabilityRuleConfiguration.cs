using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable("AvailabilityRules");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.DoctorId).IsRequired();
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.DayOfWeek).HasConversion<int>().IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time").IsRequired();
        builder.Property(x => x.EndTime).HasColumnType("time").IsRequired();
        builder.Property(x => x.Deleted).IsRequired().HasDefaultValue(false);


        // Doctor 1 → N AvailabilityRule
        builder.HasOne(x => x.Doctor)
            .WithMany(x => x.AvailabilityRules)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una regla por médico + mes + año + día.
        builder.HasIndex(x => new
        {
            x.DoctorId,
            x.Year,
            x.Month,
            x.DayOfWeek
        })
        .IsUnique();

        // Validaciones a nivel DB
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AvailabilityRule_Month",
            "[Month] BETWEEN 1 AND 12"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AvailabilityRule_Start_End",
            "[StartTime] < [EndTime]"));
    }

}

