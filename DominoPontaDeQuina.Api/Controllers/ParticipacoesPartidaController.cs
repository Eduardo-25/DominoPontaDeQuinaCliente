using DominoPontaDeQuina.Application.Services;
using DominoPontaDeQuina.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DominoPontaDeQuina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ParticipacoesPartidaController : ControllerBase
{
    private readonly ParticipacaoPartidaService _service;

    public ParticipacoesPartidaController(
        ParticipacaoPartidaService service)
    {
        _service = service;
    }

    [HttpGet("partida/{partidaId:guid}")]
    public async Task<ActionResult<List<ParticipacaoPartida>>>
        BuscarPorPartida(Guid partidaId)
    {
        return Ok(
            await _service.BuscarPorPartidaAsync(partidaId));
    }

    [HttpGet("jogador/{jogadorId:guid}")]
    public async Task<ActionResult<List<ParticipacaoPartida>>>
        BuscarPorJogador(Guid jogadorId)
    {
        return Ok(
            await _service.BuscarPorJogadorAsync(jogadorId));
    }

    [HttpPost]
    public async Task<IActionResult> Adicionar(
        [FromBody] CriarParticipacaoRequest request)
    {
        await _service.AdicionarAsync(
            request.PartidaId,
            request.JogadorId,
            request.Posicao);

        return Ok();
    }
}

public class CriarParticipacaoRequest
{
    public Guid PartidaId { get; set; }

    public Guid JogadorId { get; set; }

    public int Posicao { get; set; }
}