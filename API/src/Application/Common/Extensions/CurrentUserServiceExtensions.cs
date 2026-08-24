using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.Common.Extensions;

public static class CurrentUserServiceExtensions
{
    public static int GetRequiredCharacterId(this ICurrentUserService currentUserService)
    {
        return currentUserService.GetCharacterId() ?? throw new ForbiddenAccessException();
    }
}
