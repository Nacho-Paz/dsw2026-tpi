using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enum;
using Dsw2026Tpi.Domain.Status;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace PruebasUnitariasTPI
{
    public class AppointmentServiceTest
    {
        private readonly IPersistence _mockPersistence = Substitute.For<IPersistence>();
        private readonly ILogger<AppointmentService> _mockLogger = Substitute.For<ILogger<AppointmentService>>();

        [Fact]
        public async Task CancelAppointmentAsync_CuandoElTurnoExisteYEstaReservado_EntoncesSeCancelaYActualizaElSlot()
        {
            // Arrange
            var service = new AppointmentService(_mockPersistence, _mockLogger);
            var appointmentId = Guid.NewGuid();

            var slotSimulado = new AvailabilitySlot
            {
                Id = Guid.NewGuid(),
                Status = SlotStatus.BOOKED
            };

            var turnoSimulado = new Appointment(appointmentId,slotSimulado.Id,null);

            _mockPersistence
                .First(Arg.Any<Expression<Func<Appointment, bool>>>(), Arg.Any<string>())
                .Returns(Task.FromResult<Appointment?>(turnoSimulado));

            // Act
            await service.CancelAppointmentAsync(appointmentId);

            // Assert
            Assert.Equal(AppointmentStatus.CANCELLED, turnoSimulado.Status);
            Assert.NotNull(turnoSimulado.CancelledAt);
            Assert.Equal(SlotStatus.AVAILABLE, turnoSimulado.AvailabilitySlot.Status);

            await _mockPersistence.Received(1).Update(turnoSimulado);
            await _mockPersistence.Received(1).Update(turnoSimulado.AvailabilitySlot);
        }
    }
}