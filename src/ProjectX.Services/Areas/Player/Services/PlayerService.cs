using Microsoft.EntityFrameworkCore;
using ProjectX.Model;

namespace ProjectX.Services.Areas.Player.Services;

public interface IPlayerService
{
    Task<int> Get(int id);

    Task<int> Save();
}

public class PlayerService : IPlayerService
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(PlayerService));

    private readonly Func<ProjectXContext> _dbFactory;

    public PlayerService(Func<ProjectXContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> Get(int id)
    {
        using var db = _dbFactory();

        var result = await db.Players
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        Log.Debug("Found player for id: {0}", result);

        return result;
    }

    public async Task<int> Save()
    {
        using var db = _dbFactory();

        var player = new ProjectX.Model.DbSets.Player
        {
            ModDate = DateTime.Now
        };

        db.Players.Add(player);

        await db.SaveChangesAsync();

        Log.Debug("Saved player for id: {0}", player.Id);

        return player.Id;
    }
}
