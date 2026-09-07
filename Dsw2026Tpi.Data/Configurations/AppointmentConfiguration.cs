using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Status;

namespace Dsw2026Tpi.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.AvailabilitySlotId).IsRequired();
        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.Reason)
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(AppointmentStatus.BOOKED);

        builder.Property(x => x.CancelledAt).IsRequired(false);
        builder.Property(x => x.AttendedAt).IsRequired(false);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Appointment 1 → 1 AvailabilitySlot
        builder.HasOne(x => x.AvailabilitySlot)
            .WithOne(x => x.Appointment)
            .HasForeignKey<Appointment>(x => x.AvailabilitySlotId)
            .OnDelete(DeleteBehavior.Restrict);

        // Patient 1 → N Appointment
        builder.HasOne(x => x.Patient)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }

}
