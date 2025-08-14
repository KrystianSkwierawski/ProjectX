using Microsoft.AspNetCore.Mvc;
using ProjectX.Services.Areas.Services;

namespace ProjectX.API.Areas.Player;

[Route("api/player")]
[ApiController]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayerController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlayer()
    {
        var result = await _playerService.Get();
        return Ok(result);
    }
}
