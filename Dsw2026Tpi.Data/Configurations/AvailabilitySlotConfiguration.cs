using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

internal class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.ToTable("AvailabilitySlots");
        builder.HasKey(a => a.Id);
        builder.Property(x => x.AvailabilityRuleId).IsRequired();
        builder.Property(s => s.SlotDate).IsRequired();
        builder.Property(a => a.StartTime).HasColumnType("time").IsRequired();
        builder.Property(a => a.EndTime).HasColumnType("time").IsRequired();
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(SlotStatus.AVAILABLE)
            .IsRequired();
        builder.Property(x => x.Deleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        // AvailabilityRule 1 → N AvailabilitySlot
        builder.HasOne(x => x.AvailabilityRule)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.AvailabilityRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        // No puede existir el mismo slot dos veces
        builder.HasIndex(x => new
        {
            x.AvailabilityRuleId,
            x.SlotDate,
            x.StartTime
        })
        .IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AvailabilitySlot_Start_End",
            "[StartTime] < [EndTime]"));
    }
}
