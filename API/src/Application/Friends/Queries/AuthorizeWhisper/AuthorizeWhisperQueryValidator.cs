using FluentValidation;

namespace ProjectX.Application.Friends.Queries.AuthorizeWhisper;

public class AuthorizeWhisperQueryValidator : AbstractValidator<AuthorizeWhisperQuery>
{
    public AuthorizeWhisperQueryValidator()
    {
        RuleFor(x => x.CharacterId).GreaterThan(0);
    }
}
