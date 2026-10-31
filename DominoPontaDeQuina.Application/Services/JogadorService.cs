using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Interfaces;

namespace DominoPontaDeQuina.Application.Services;

public class JogadorService
{
    private readonly IJogadorRepository _jogadorRepository;

    public JogadorService(IJogadorRepository jogadorRepository)
    {
        _jogadorRepository = jogadorRepository;
    }

    public async Task<List<Jogador>> ListarAsync()
    {
        return await _jogadorRepository.ListarAsync();
    }

    public async Task<List<Jogador>> BuscarPorNomeAsync(string nome)
    {
        return await _jogadorRepository.BuscarPorNomeAsync(nome);
    }

    public async Task CriarAsync(
        string nomeExibicao,
        Guid usuarioId)
    {
        var jogador = new Jogador
        {
            NomeExibicao = nomeExibicao,
            UsuarioId = usuarioId
        };

        await _jogadorRepository.AdicionarAsync(jogador);
    }
}