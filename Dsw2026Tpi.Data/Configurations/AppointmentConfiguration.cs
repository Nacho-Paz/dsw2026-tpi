<<<<<<< HEAD
﻿using System;
=======
﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dsw2026Tpi.Domain.Entities;
using System;
>>>>>>> origin/feature/integration
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Data.Configurations
{
<<<<<<< HEAD
    internal class AppointmentConfiguration
    {
=======
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("APPOINTMENTS");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AvailabilitySlotId).IsRequired();
            builder.Property(x => x.PatientId).IsRequired();

            builder.Property(x => x.Reason)
                   .HasMaxLength(300)
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasMaxLength(20)
                   .IsRequired()
                   .HasDefaultValue("BOOKED"); // TODO:revisar Estados con enum

            builder.Property(x => x.CancelledAt).IsRequired(false);
            builder.Property(x => x.AttendedAt).IsRequired(false);

            builder.Property(x => x.RowVersion)
                   .IsRowVersion();

            builder.HasOne(x => x.AvailabilitySlot)
                   .WithOne(x => x.Appointment)
                   .HasForeignKey<Appointment>(x => x.AvailabilitySlotId);
        }

>>>>>>> origin/feature/integration
    }
}
