using ProjectX.Domain.Enums;

namespace ProjectX.Application.ApplicationUsers.Commands.RefreshSession;

public class RefreshSessionDto
{
    public required string Token { get; set; }

    public LanguageEnum Language { get; set; }

    public override string ToString()
    {
        return $"{nameof(RefreshSessionDto)} {{ Language = {Language} }}";
    }
}
