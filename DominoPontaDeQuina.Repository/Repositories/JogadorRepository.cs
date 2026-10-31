using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Context;
using Microsoft.EntityFrameworkCore;
using DominoPontaDeQuina.Repository.Interfaces;

namespace DominoPontaDeQuina.Repository.Repositories;

public class JogadorRepository : IJogadorRepository
{
    private readonly DominoDbContext _context;

    public JogadorRepository(DominoDbContext context)
    {
        _context = context;
    }

    public async Task<Jogador?> BuscarPorIdAsync(Guid id)
    {
        return await _context.Jogadores
            .Include(j => j.Usuario)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<List<Jogador>> ListarAsync()
    {
        return await _context.Jogadores
            .Include(j => j.Usuario)
            .OrderBy(j => j.NomeExibicao)
            .ToListAsync();
    }

    public async Task<List<Jogador>> BuscarPorNomeAsync(string nome)
    {
        return await _context.Jogadores
            .Where(j => j.NomeExibicao.Contains(nome))
            .OrderBy(j => j.NomeExibicao)
            .ToListAsync();
    }

    public async Task<List<Jogador>> BuscarPorUsuarioAsync(Guid usuarioId)
    {
        return await _context.Jogadores
            .Where(j => j.UsuarioId == usuarioId)
            .OrderBy(j => j.NomeExibicao)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Jogador jogador)
    {
        await _context.Jogadores.AddAsync(jogador);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Jogador jogador)
    {
        _context.Jogadores.Update(jogador);
        await _context.SaveChangesAsync();
    }

    public async Task ExcluirAsync(Guid id)
    {
        var jogador = await BuscarPorIdAsync(id);

        if (jogador is null)
            return;

        _context.Jogadores.Remove(jogador);
        await _context.SaveChangesAsync();
    }
}