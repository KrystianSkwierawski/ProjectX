namespace ProjectX.Services.Areas.Services;

public interface IPlayerService
{
    Task<int> Get();
}

public class PlayerService : IPlayerService
{
    public async Task<int> Get()
    {
        return await Task.Run(() => 1);
    }
}
