namespace ProjectX.Domain.Common;

public abstract class BaseAuditableEntity
{
    public DateTimeOffset ModDate { get; set; }
}
