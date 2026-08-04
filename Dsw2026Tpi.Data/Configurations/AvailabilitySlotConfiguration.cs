using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dsw2026Tpi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Status;
using Dsw2026Tpi.Domain.Enum;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
    {
        public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
        {
            builder.ToTable("AVAILABILITYSLOTS");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AvailabilityRuleId).IsRequired();
            builder.Property(x => x.SlotDate).HasColumnType("date").IsRequired();
            builder.Property(x => x.StartTime).HasColumnType("time").IsRequired();
            builder.Property(x => x.EndTime).HasColumnType("time").IsRequired();

            builder.Property(x => x.Status)
                   .HasMaxLength(20)
                   .IsRequired()
                   .HasDefaultValue(SlotStatus.AVAILABLE);

            builder.Property(x => x.Deleted).HasDefaultValue(false);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasIndex(x => new { x.AvailabilityRuleId, x.SlotDate, x.StartTime })
                   .IsUnique()// TODO: revisar todo
                   .HasDatabaseName("UNIQUE_Rule_DateTime");

            builder.HasIndex(x => new { x.DoctorId, x.SlotDate, x.StartTime }) 
                .IsUnique()
                .HasFilter("[Deleted] = 0")
                .HasDatabaseName("UNIQUE_Doctor_Slot_Time");

            builder.HasOne(x => x.AvailabilityRule)
                   .WithMany(x => x.Slots)
                   .HasForeignKey(x => x.AvailabilityRuleId);
        }
    }
}
