using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace DominoPontaDeQuina.Application.Services;

public class UsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<Usuario>> ListarAsync()
    {
        return await _usuarioRepository.ListarAsync();
    }

    public async Task<Usuario?> BuscarPorIdAsync(Guid id)
    {
        return await _usuarioRepository.BuscarPorIdAsync(id);
    }

    public async Task CriarAsync(
        string nome,
        string email,
        string senha)
    {
        var usuarioExistente =
            await _usuarioRepository.BuscarPorEmailAsync(email);

        if (usuarioExistente is not null)
        {
            throw new InvalidOperationException(
                "Já existe um usuário com este e-mail.");
        }

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email
        };

        usuario.HashSenha =
            _passwordHasher.HashPassword(usuario, senha);

        await _usuarioRepository.AdicionarAsync(usuario);
    }
}