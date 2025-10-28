namespace ProjectX.Application.Common.Interfaces;
public interface ICurrentUserService
{
    string GetId();

    List<string>? Roles { get; }
}
