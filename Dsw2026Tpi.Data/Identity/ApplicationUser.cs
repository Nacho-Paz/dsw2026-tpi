using Microsoft.AspNetCore.Identity;

namespace Dsw2026Tpi.Data.Identity;

public class ApplicationUser: IdentityUser
{
    public bool IsActive { get; set; } = true;
    public string? Dni { get; set; } //TODO: Ver que onda esto, no me conviene que este en patient?
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
