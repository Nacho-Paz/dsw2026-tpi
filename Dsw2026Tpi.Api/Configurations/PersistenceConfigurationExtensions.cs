using Dsw2026Tpi.Data;
using Dsw2026Tpi.Data.Extensions;
using Dsw2026Tpi.Data.Identity;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Api.Configurations;

public static class PersistenceConfigurationExtensions
{
    public static IServiceCollection AddApplicationPersistence(this IServiceCollection services,
        IConfiguration configuration)
    {
        //Obtener cadena de conexión desde appsettings.json
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        //Agregar contexto (O/RM) y utilizar SQL Server para DB
        services.AddDbContext<Dsw2026TpiDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.UseSeeding((c, _) =>
            {
                c.Seedwork<Speciality>("Sources\\specialities.json");
                c.Seedwork<Doctor>("Sources\\doctors.json");
            });
        });

        //peremite qjue cualquier servicio reciba el contexto de la Bd
        services.AddDbContext<AuthenticationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.UseSeeding((c, t) =>
            {
                c.Seedwork<IdentityRole>("Sources\\roles.json");
            });
        });
        return services;
    }
}
