using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Identity;
public class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        Characters = new HashSet<Character>();
    }

    [InverseProperty(nameof(Character.ApplicationUser))]
    public virtual ICollection<Character> Characters { get; set; }
}
