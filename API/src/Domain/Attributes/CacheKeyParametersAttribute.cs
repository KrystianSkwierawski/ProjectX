namespace ProjectX.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class CacheKeyParametersAttribute : Attribute
{
    public required string Format { get; set; }

    public int ExpiryInSeconds { get; set; }
}
