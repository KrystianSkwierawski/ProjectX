namespace ProjectX.Application.Common.Interfaces;
public interface ICurrentUserService
{
    string? Id { get; }

    List<string>? Roles { get; }
}
