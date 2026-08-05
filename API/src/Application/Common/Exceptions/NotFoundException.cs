namespace ProjectX.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName)
        : base($"The requested {resourceName} was not found.")
    {
        ResourceName = resourceName;
    }

    public string ResourceName { get; }
}
