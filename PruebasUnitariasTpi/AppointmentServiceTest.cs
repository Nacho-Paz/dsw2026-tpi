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
using Dsw2026Tpi.CrossCutting.Exceptions;

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
            var patientUserId = Guid.NewGuid();
            var patient = new Patient(patientUserId, "12345678");

            var slotSimulado = new AvailabilitySlot
            {
                Id = Guid.NewGuid(),
                Status = SlotStatus.BOOKED
            };

            var turnoSimulado = new Appointment(patient.Id, slotSimulado.Id, "Consulta")
            {
                Patient = patient,
                AvailabilitySlot = slotSimulado
            };

            _mockPersistence
                .First(Arg.Any<Expression<Func<Appointment, bool>>>(), Arg.Any<string[]>())
                .Returns(Task.FromResult<Appointment?>(turnoSimulado));

            // Act
            await service.CancelAppointmentAsync(turnoSimulado.Id, patientUserId);

            // Assert
            Assert.Equal(AppointmentStatus.CANCELLED, turnoSimulado.Status);
            Assert.NotNull(turnoSimulado.CancelledAt);
            Assert.Equal(SlotStatus.AVAILABLE, turnoSimulado.AvailabilitySlot.Status);

            await _mockPersistence.Received(1).Update(turnoSimulado);
            await _mockPersistence.Received(1).Update(turnoSimulado.AvailabilitySlot);
        }

        [Fact]
        public async Task CancelAppointmentAsync_CuandoElTurnoEsDeOtroPaciente_EntoncesLanzaAuthorizationException()
        {
            // Arrange
            var service = new AppointmentService(_mockPersistence, _mockLogger);
            var patient = new Patient(Guid.NewGuid(), "12345678");
            var turnoSimulado = new Appointment(patient.Id, Guid.NewGuid(), "Consulta")
            {
                Patient = patient,
                AvailabilitySlot = new AvailabilitySlot { Status = SlotStatus.BOOKED }
            };

            _mockPersistence
                .First(Arg.Any<Expression<Func<Appointment, bool>>>(), Arg.Any<string[]>())
                .Returns(Task.FromResult<Appointment?>(turnoSimulado));

            // Act / Assert
            await Assert.ThrowsAsync<AuthorizationException>(
                () => service.CancelAppointmentAsync(turnoSimulado.Id, Guid.NewGuid()));

            await _mockPersistence.DidNotReceive().Update(Arg.Any<Appointment>());
        }
    }
}
