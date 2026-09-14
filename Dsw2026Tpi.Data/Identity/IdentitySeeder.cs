using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace Dsw2026Tpi.Data.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        var administrators = await userManager.GetUsersInRoleAsync(Roles.Administrator);

        if (administrators.Count > 0)
        {
            // Ya existe al menos un administrador. No creamos otro cada vez que arranca la aplicación.
            return;
        }

        //Crear las credenciales del administrador inicial
        var email = "admin@dws2026.prueba";
        var password = GenerateSecurePassword();

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Deleted = false
        };

        var result = await userManager.CreateAsync(admin, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"No se pudo crear el administrador inicial. Errores: {errors}");
        }

        //Asignarle el rol Administrador
        var roleResult = await userManager.AddToRoleAsync(admin, Roles.Administrator);

        if (!roleResult.Succeeded)
        {
            // Si por alguna razón falla la asignación del rol, eliminamos el usuario para no dejar un admin inválido.
            await userManager.DeleteAsync(admin);

            var errors = string.Join("; ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"No se pudo asignar el rol Administrador al usuario inicial. " + $"Errores: {errors}");
        }

        Console.WriteLine();
        Console.WriteLine("==============================================");
        Console.WriteLine("     ADMINISTRADOR INICIAL CREADO");
        Console.WriteLine("==============================================");
        Console.WriteLine($"Email:    {email}");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine("==============================================");
        Console.WriteLine("GUARDE ESTAS CREDENCIALES.");
        Console.WriteLine("La contraseña no se volverá a mostrar.");
        Console.WriteLine("==============================================");
        Console.WriteLine();
    }

    private static string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()-_=+";

        const int passwordLength = 16;

        var allCharacters = upper + lower + digits + special;

        var password = new char[passwordLength];

        password[0] = GetRandomCharacter(upper);
        password[1] = GetRandomCharacter(lower);
        password[2] = GetRandomCharacter(digits);
        password[3] = GetRandomCharacter(special);

        for (var i = 4; i < passwordLength; i++)
        {
            password[i] = GetRandomCharacter(allCharacters);
        }

        // Mezclamos los caracteres usando un orden aleatorio seguro.
        return new string(password.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }

    private static char GetRandomCharacter(string characters)
    {
        return characters[RandomNumberGenerator.GetInt32(characters.Length)];
    }
}
