namespace Dsw2026Tpi.Application.Dtos;

//CHECK: Ready
public record RegisterModel
{
    public record Request(string Email, string Password);
    public record Response(string Email);
}
