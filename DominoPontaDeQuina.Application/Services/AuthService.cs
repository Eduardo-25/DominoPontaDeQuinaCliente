using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DominoPontaDeQuina.Application.Services;

public class AuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher<Usuario> passwordHasher,
        IConfiguration configuration)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<string?> LoginAsync(
        string email,
        string senha)
    {
        var usuario =
            await _usuarioRepository.BuscarPorEmailAsync(email);

        if (usuario is null)
            return null;

        var resultado = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.HashSenha,
            senha);

        if (resultado == PasswordVerificationResult.Failed)
            return null;

        return GerarToken(usuario);
    }

    private string GerarToken(Usuario usuario)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Chave JWT não configurada.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Issuer JWT não configurado.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Audience JWT não configurada.");

        var expirationMinutes =
            int.TryParse(
                _configuration["Jwt:ExpirationMinutes"],
                out var minutos)
                ? minutos
                : 60;

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                usuario.Id.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Email,
                usuario.Email),

            new Claim(
                ClaimTypes.Name,
                usuario.Nome)
        };

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}