using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Context;
using Microsoft.EntityFrameworkCore;
using DominoPontaDeQuina.Repository.Interfaces;

namespace DominoPontaDeQuina.Repository.Repositories;
public class PartidaRepository : IPartidaRepository

{
    private readonly DominoDbContext _context;

    public PartidaRepository(DominoDbContext context)
    {
        _context = context;
    }

    public async Task<Partida?> BuscarPorIdAsync(Guid id)
    {
        return await _context.Partidas
            .Include(p => p.Participacoes)
            .ThenInclude(pp => pp.Jogador)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Partida>> ListarAsync()
    {
        return await _context.Partidas
            .OrderByDescending(p => p.IniciadaEm)
            .ToListAsync();
    }

    public async Task<List<Partida>> BuscarPorStatusAsync(
        StatusPartida status)
    {
        return await _context.Partidas
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.IniciadaEm)
            .ToListAsync();
    }

    public async Task<List<Partida>> ListarEmAndamentoAsync()
    {
        return await _context.Partidas
            .Where(p => p.Status == StatusPartida.EmAndamento)
            .OrderBy(p => p.IniciadaEm)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Partida partida)
    {
        await _context.Partidas.AddAsync(partida);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Partida partida)
    {
        _context.Partidas.Update(partida);
        await _context.SaveChangesAsync();
    }

    public async Task ExcluirAsync(Guid id)
    {
        var partida = await BuscarPorIdAsync(id);

        if (partida is null)
            return;

        _context.Partidas.Remove(partida);
        await _context.SaveChangesAsync();
    }
}