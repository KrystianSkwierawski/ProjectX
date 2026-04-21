using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    public class LoginApplicationUserDto
    {
        public string Token { get; set; }

        public LanguageEnum Language { get; set; }
    }
}