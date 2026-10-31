using DominoPontaDeQuina.Application.Services;
using DominoPontaDeQuina.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DominoPontaDeQuina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PartidasController : ControllerBase
{
    private readonly PartidaService _partidaService;

    public PartidasController(PartidaService partidaService)
    {
        _partidaService = partidaService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Partida>>> Listar()
    {
        var partidas = await _partidaService.ListarAsync();

        return Ok(partidas);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Partida>> Buscar(Guid id)
    {
        var partida = await _partidaService.BuscarPorIdAsync(id);

        if (partida is null)
            return NotFound();

        return Ok(partida);
    }

    [HttpGet("em-andamento")]
    public async Task<ActionResult<List<Partida>>> EmAndamento()
    {
        var partidas =
            await _partidaService.ListarEmAndamentoAsync();

        return Ok(partidas);
    }

    [HttpPost]
    public async Task<ActionResult<Partida>> Criar()
    {
        var partida = await _partidaService.CriarAsync();

        return CreatedAtAction(
            nameof(Buscar),
            new { id = partida.Id },
            partida);
    }

    [HttpPut("{id:guid}/iniciar")]
    public async Task<IActionResult> Iniciar(Guid id)
    {
        try
        {
            await _partidaService.IniciarAsync(id);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid id)
    {
        try
        {
            await _partidaService.FinalizarAsync(id);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}