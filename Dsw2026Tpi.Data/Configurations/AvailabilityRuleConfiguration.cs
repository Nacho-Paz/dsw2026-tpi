using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AvailabilityRuleConfiguration: IEntityTypeConfiguration<AvailabilityRule>
        {
            public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
            {
                builder.ToTable("AVAILABILITYRULES");

                builder.HasKey(x => x.Id);

                builder.Property(x => x.DoctorId).IsRequired();
                builder.Property(x => x.Month).IsRequired();
                builder.Property(x => x.Year).IsRequired();
                builder.Property(x => x.DayOfWeek).IsRequired();
                builder.Property(x => x.StartTime).HasColumnType("time").IsRequired();
                builder.Property(x => x.EndTime).HasColumnType("time").IsRequired();
                builder.Property(x => x.Deleted).HasDefaultValue(false);

                builder.HasIndex(x => new { x.DoctorId, x.Year, x.Month, x.DayOfWeek, x.StartTime, x.EndTime })
                       .IsUnique()
                       .HasDatabaseName("UNIQUE_Doctor_Time");

                builder.HasOne(x => x.Doctor)
                       .WithMany()
                       .HasForeignKey(x => x.DoctorId);
            }

        }
    }
