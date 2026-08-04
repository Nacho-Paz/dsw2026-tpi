using Microsoft.AspNetCore.Identity;

namespace Dsw2026Tpi.Data.Identity;

public class ApplicationUser: IdentityUser
{
    public bool Deleted { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
