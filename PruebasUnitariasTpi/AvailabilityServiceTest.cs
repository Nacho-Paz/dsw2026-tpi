using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace PruebasUnitariasTpi
{
    public class AvailabilityServiceTest
    {
        private readonly IPersistence _mockPersistence = Substitute.For<IPersistence>();
        private readonly ILogger<AvailabilityService> _mockLogger = Substitute.For<ILogger<AvailabilityService>>();

        [Fact]
        public async Task CreateAvailabilitiesAsync_CuandoElDoctorNoExiste_EntoncesProduceUnaExcepcion()
        {
            // Arrange
            var service = new AvailabilityService(_mockPersistence, _mockLogger);

            var request = new AvailabilityModel.Request(
                Guid.NewGuid(), 
                new List<AvailabilityModel.DayRule> 
                {
                    new AvailabilityModel.DayRule("LUNES", "08:00", "12:00")
                }
            );

            _mockPersistence
                .First(Arg.Any<Expression<Func<Doctor, bool>>>())
                .Returns(Task.FromResult<Doctor?>(null));

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            {
                await service.CreateAvailabilitiesAsync(request);
            });
        }
    }
}

