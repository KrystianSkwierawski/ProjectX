using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class LoginApplicationUserDto
    {
        public string token;

        public LanguageEnum language;
    }
}