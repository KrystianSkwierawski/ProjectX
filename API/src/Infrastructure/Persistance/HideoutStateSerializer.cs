using System.Text.Json;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Infrastructure.Persistance;

public sealed class HideoutStateSerializer : IHideoutStateSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public T Deserialize<T>(string json) where T : class
    {
        return JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new JsonException($"Hideout state for {typeof(T).Name} must not be null.");
    }

    public string Serialize<T>(T state) where T : class => JsonSerializer.Serialize(state, Options);
}
