using Microsoft.AspNetCore.Identity;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;
public class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        Characters = new HashSet<Character>();
    }

    public LanguageEnum Language { get; set; }

    public virtual ICollection<Character> Characters { get; set; }
}
