using Microsoft.EntityFrameworkCore;
using ProjectX.Model;
using ProjectX.Model.DbSets;

namespace ProjectX.Services.Areas.Player.Services;

public interface IPlayerPositionService
{
    Task<int> Get(int id);

    Task<int> Save(int playerId);
}

public class PlayerPositionService : IPlayerPositionService
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(PlayerPositionService));

    private readonly Func<ProjectXContext> _dbFactory;

    public PlayerPositionService(Func<ProjectXContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> Get(int id)
    {
        using var db = _dbFactory();

        var result = await db.PlayerPositions
            .Where(x => x.Id == id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        Log.Debug("Found player for id: {0}", result);

        return result;
    }

    public async Task<int> Save(int playerId)
    {
        using var db = _dbFactory();

        var playerPosition = new PlayerPosition
        {
            ModDate = DateTime.Now,
            PlayerId = playerId
        };

        db.PlayerPositions.Add(playerPosition);

        await db.SaveChangesAsync();

        Log.Debug("Saved player position for id: {0}", playerPosition.Id);

        return playerPosition.Id;
    }
}
