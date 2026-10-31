using DominoPontaDeQuina.Application.Services;
using DominoPontaDeQuina.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DominoPontaDeQuina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JogadoresController : ControllerBase
{
    private readonly JogadorService _jogadorService;

    public JogadoresController(JogadorService jogadorService)
    {
        _jogadorService = jogadorService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Jogador>>> Listar()
    {
        return Ok(await _jogadorService.ListarAsync());
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<Jogador>>> Buscar(
        [FromQuery] string nome)
    {
        return Ok(
            await _jogadorService.BuscarPorNomeAsync(nome));
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarJogadorRequest request)
    {
        await _jogadorService.CriarAsync(
            request.NomeExibicao,
            request.UsuarioId);

        return Ok();
    }
}

public class CriarJogadorRequest
{
    public string NomeExibicao { get; set; } = string.Empty;

    public Guid UsuarioId { get; set; }
}