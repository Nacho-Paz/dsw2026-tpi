using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dsw2026Tpi.Domain.Entities;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Status;
using Dsw2026Tpi.Domain.Enum;

namespace Dsw2026Tpi.Data.Configurations
{
    internal class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
    {
        public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
        {
            builder.ToTable("AvailabilitySlots");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(a => a.StartTime).IsRequired();
            builder.Property(s => s.SlotDate).IsRequired();
            builder.Property(a => a.EndTime).IsRequired();
            builder.HasOne(a => a.AvailabilityRule).WithMany(a => a.Slots).HasForeignKey(a => a.AvailabilityRuleId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.AvailabilityRuleId, x.SlotDate, x.StartTime });
            builder.Property(x => x.Deleted).HasDefaultValue(false);
            builder.Property(x => x.RowVersion).IsRowVersion();
        }
    }
}
