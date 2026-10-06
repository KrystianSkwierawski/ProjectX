namespace ProjectX.Application.Common.Interfaces;

public interface IHideoutStateSerializer
{
    T Deserialize<T>(string json) where T : class;

    string Serialize<T>(T state) where T : class;
}
