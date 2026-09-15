using Microsoft.AspNetCore.Identity;

namespace Dsw2026Tpi.Data.Identity;

//CHECK: Ready
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}
