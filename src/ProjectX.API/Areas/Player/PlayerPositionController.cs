using Microsoft.AspNetCore.Mvc;
using ProjectX.Services.Areas.Player.Services;

namespace ProjectX.API.Areas.Player;

[Route("api/player-positions")]
[ApiController]
public class PlayerPositionController : ControllerBase
{
    private readonly IPlayerPositionService _playerPositionService;

    public PlayerPositionController(IPlayerPositionService playerPositionService)
    {
        _playerPositionService = playerPositionService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await _playerPositionService.Get(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save(int playerId)
    {
        var result = await _playerPositionService.Save(playerId);
        return Ok(result);
    }
}
