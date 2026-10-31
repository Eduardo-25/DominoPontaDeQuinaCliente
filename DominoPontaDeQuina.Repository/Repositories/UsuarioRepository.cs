using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Context;
using Microsoft.EntityFrameworkCore;
using DominoPontaDeQuina.Repository.Interfaces;

namespace DominoPontaDeQuina.Repository.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly DominoDbContext _context;

    public UsuarioRepository(DominoDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> BuscarPorIdAsync(Guid id)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario?> BuscarPorEmailAsync(string email)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<List<Usuario>> ListarAsync()
    {
        return await _context.Usuarios
            .OrderBy(u => u.Nome)
            .ToListAsync();
    }

    public async Task<List<Usuario>> BuscarPorNomeAsync(string nome)
    {
        return await _context.Usuarios
            .Where(u => u.Nome.Contains(nome))
            .OrderBy(u => u.Nome)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Usuario usuario)
    {
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();
    }

    public async Task ExcluirAsync(Guid id)
    {
        var usuario = await BuscarPorIdAsync(id);

        if (usuario is null)
            return;

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
    }
}