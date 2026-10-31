using DominoPontaDeQuina.Application.Services;
using DominoPontaDeQuina.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DominoPontaDeQuina.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _usuarioService;

    public UsuariosController(UsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Usuario>>> Listar()
    {
        return Ok(await _usuarioService.ListarAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Usuario>> Buscar(Guid id)
    {
        var usuario = await _usuarioService.BuscarPorIdAsync(id);

        if (usuario is null)
            return NotFound();

        return Ok(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioRequest request)
    {
        try
        {
            await _usuarioService.CriarAsync(
                request.Nome,
                request.Email,
                request.Senha);

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}

public class CriarUsuarioRequest
{
    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;
}