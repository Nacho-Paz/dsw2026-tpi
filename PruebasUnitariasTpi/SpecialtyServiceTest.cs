using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;

namespace PruebasUnitariasTpi
{
    public class SpecialtyServiceTests
    {
        private readonly IPersistence _mockPersistence = Substitute.For<IPersistence>();
        private readonly ILogger<SpecialtyService> _mockLogger = Substitute.For<ILogger<SpecialtyService>>();

        [Fact]
        public async Task DeleteSpecialty_CuandoLaEspecialidadNoExiste_EntoncesLanzaEntityNotFoundException()
        {
            var service = new SpecialtyService(_mockPersistence, _mockLogger);
            var specialtyId = Guid.NewGuid();

            Specialty? specialtyNull = null;
            _mockPersistence.First<Specialty>(Arg.Any<System.Linq.Expressions.Expression<Func<Specialty, bool>>>())
                .Returns(Task.FromResult(specialtyNull));

            await Assert.ThrowsAsync<EntityNotFoundException>(() => service.DeleteSpecialty(specialtyId));
        }
    }
}
