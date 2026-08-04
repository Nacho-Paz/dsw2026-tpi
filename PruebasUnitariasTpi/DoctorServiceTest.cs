using System;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.CrossCutting.Exceptions;


namespace PruebasUnitariasTpi
{
    public class DoctorServiceTest
    {
        private readonly IPersistence _mockPersistence = Substitute.For<IPersistence>();
        private readonly ILogger<DoctorService> _mockLogger = Substitute.For<ILogger<DoctorService>>();
        [Fact]
        public async Task CreateDoctor_CuandoElNombreEsValidoYLaEspecialidadExiste_EntoncesCreaElMedico()
        {
            var service = new DoctorService(_mockPersistence, _mockLogger);
            var specialityId = Guid.NewGuid();
            var speciality = new Specialty("Cardiología", "Especialidad en cardiología general.");

            var request = new DoctorModel.Request(
            
                Name: "Juan Pérez",
                LicenseNumber: "MP12345",
                SpecialityId: specialityId
            );

            _mockPersistence.First<Specialty>(Arg.Any<System.Linq.Expressions.Expression<Func<Specialty, bool>>>())
                .Returns(Task.FromResult<Specialty?>(speciality));

            var result = await service.CreateDoctor(request);

            Assert.NotNull(result);
            Assert.Equal("Juan Pérez", result.Name);
            await _mockPersistence.Received().Add(Arg.Any<Doctor>());
        }
    }
}
