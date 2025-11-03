using Microsoft.AspNetCore.Identity;

namespace ProjectX.Domain.Entities;
public class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        Characters = new HashSet<Character>();
    }

    public virtual ICollection<Character> Characters { get; set; }
}
