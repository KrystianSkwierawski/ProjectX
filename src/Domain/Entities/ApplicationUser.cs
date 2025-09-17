using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ProjectX.Domain.Entities;
public class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        Characters = new HashSet<Character>();
    }

    [InverseProperty(nameof(Character.ApplicationUser))]
    public virtual ICollection<Character> Characters { get; set; }
}
