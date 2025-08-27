﻿namespace ProjectX.API.Infrastructure;

public abstract class EndpointGroupBase
{
    public virtual string? GroupName { get; }

    public abstract void Map(RouteGroupBuilder groupBuilder);
}
