using Microsoft.AspNetCore.Identity;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        LockoutEnabled = true;
    }

    public LanguageEnum Language { get; set; }

    public ICollection<Character> Characters { get; private set; } = new HashSet<Character>();
}
