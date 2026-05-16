using Assets.Scripts.Areas.Shared.Enums;

namespace Assets.Scripts.Areas.Character.Models
{
    public class LoginApplicationUserDto
    {
        public string Token { get; set; }

        public LanguageEnum Language { get; set; }
    }
}