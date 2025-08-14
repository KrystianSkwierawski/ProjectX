using Microsoft.AspNetCore.Mvc;
using ProjectX.Services.Areas.Player.Services;

namespace ProjectX.API.Areas.Player;

[Route("api/players")]
[ApiController]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayerController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await _playerService.Get(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save()
    {
        var result = await _playerService.Save();
        return Ok(result);
    }
}
