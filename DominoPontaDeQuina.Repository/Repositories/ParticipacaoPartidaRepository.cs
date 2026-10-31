using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Context;
using Microsoft.EntityFrameworkCore;
using DominoPontaDeQuina.Repository.Interfaces;

namespace DominoPontaDeQuina.Repository.Repositories;
public class ParticipacaoPartidaRepository : IParticipacaoPartidaRepository

{
    private readonly DominoDbContext _context;

    public ParticipacaoPartidaRepository(DominoDbContext context)
    {
        _context = context;
    }

    public async Task<ParticipacaoPartida?> BuscarPorIdAsync(Guid id)
    {
        return await _context.ParticipacoesPartidas
            .Include(pp => pp.Partida)
            .Include(pp => pp.Jogador)
            .FirstOrDefaultAsync(pp => pp.Id == id);
    }

    public async Task<List<ParticipacaoPartida>> ListarAsync()
    {
        return await _context.ParticipacoesPartidas
            .Include(pp => pp.Partida)
            .Include(pp => pp.Jogador)
            .OrderBy(pp => pp.Posicao)
            .ToListAsync();
    }

    public async Task<List<ParticipacaoPartida>> BuscarPorPartidaAsync(
        Guid partidaId)
    {
        return await _context.ParticipacoesPartidas
            .Where(pp => pp.PartidaId == partidaId)
            .Include(pp => pp.Jogador)
            .OrderBy(pp => pp.Posicao)
            .ToListAsync();
    }

    public async Task<List<ParticipacaoPartida>> BuscarPorJogadorAsync(
        Guid jogadorId)
    {
        return await _context.ParticipacoesPartidas
            .Where(pp => pp.JogadorId == jogadorId)
            .Include(pp => pp.Partida)
            .OrderByDescending(pp => pp.Partida.IniciadaEm)
            .ToListAsync();
    }

    public async Task<List<ParticipacaoPartida>> ListarVencedoresAsync()
    {
        return await _context.ParticipacoesPartidas
            .Where(pp => pp.Vencedor)
            .Include(pp => pp.Jogador)
            .Include(pp => pp.Partida)
            .ToListAsync();
    }

    public async Task AdicionarAsync(ParticipacaoPartida participacao)
    {
        await _context.ParticipacoesPartidas.AddAsync(participacao);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(ParticipacaoPartida participacao)
    {
        _context.ParticipacoesPartidas.Update(participacao);
        await _context.SaveChangesAsync();
    }

    public async Task ExcluirAsync(Guid id)
    {
        var participacao = await BuscarPorIdAsync(id);

        if (participacao is null)
            return;

        _context.ParticipacoesPartidas.Remove(participacao);
        await _context.SaveChangesAsync();
    }
}