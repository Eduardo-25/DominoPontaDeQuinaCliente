using DominoPontaDeQuina.Repository.Services;

namespace DominoPontaDeQuina.App;

public class App
{
    private readonly UsuarioService _usuarioService;
    private readonly JogadorService _jogadorService;
    private readonly PartidaService _partidaService;
    private readonly ParticipacaoPartidaService
        _participacaoPartidaService;

    public App(
        UsuarioService usuarioService,
        JogadorService jogadorService,
        PartidaService partidaService,
        ParticipacaoPartidaService participacaoPartidaService)
    {
        _usuarioService = usuarioService;
        _jogadorService = jogadorService;
        _partidaService = partidaService;
        _participacaoPartidaService =
            participacaoPartidaService;
    }

    public async Task ExecutarAsync()
    {
        var partidas = await _partidaService.ListarAsync();

        Console.WriteLine(
            $"Total de partidas: {partidas.Count}");
    }
}