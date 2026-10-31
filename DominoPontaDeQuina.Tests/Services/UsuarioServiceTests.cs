using DominoPontaDeQuina.Application.Services;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace DominoPontaDeQuina.Tests.Services;

public class UsuarioServiceTests
{
    [Fact]
    public async Task CriarAsync_EmailNovo_DeveConsultarEAdicionarUsuario()
    {
        // Arrange
        const string nome = "Eduardo";
        const string email = "eduardo@email.com";
        const string senha = "123456";

        var repositorio = new Mock<IUsuarioRepository>();
        var passwordHasher = new Mock<IPasswordHasher<Usuario>>();

        repositorio
            .Setup(r => r.BuscarPorEmailAsync(email))
            .ReturnsAsync((Usuario?)null);

        repositorio
            .Setup(r => r.AdicionarAsync(It.IsAny<Usuario>()))
            .Returns(Task.CompletedTask);

        passwordHasher
            .Setup(h => h.HashPassword(It.IsAny<Usuario>(), senha))
            .Returns("hash-de-teste");

        var servico = new UsuarioService(
            repositorio.Object,
            passwordHasher.Object
        );

        // Act
        await servico.CriarAsync(nome, email, senha);

        // Assert
        repositorio.Verify(
            r => r.BuscarPorEmailAsync(email),
            Times.Once
        );

        repositorio.Verify(
            r => r.AdicionarAsync(It.Is<Usuario>(
                u => u.Nome == nome
                  && u.Email == email
                  && u.HashSenha == "hash-de-teste"
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task CriarAsync_EmailExistente_DeveLancarExcecaoENaoSalvar()
    {
        // Arrange
        const string email = "eduardo@email.com";

        var usuarioExistente = new Usuario
        {
            Nome = "Eduardo",
            Email = email
        };

        var repositorio = new Mock<IUsuarioRepository>();
        var passwordHasher = new Mock<IPasswordHasher<Usuario>>();

        repositorio
            .Setup(r => r.BuscarPorEmailAsync(email))
            .ReturnsAsync(usuarioExistente);

        var servico = new UsuarioService(
            repositorio.Object,
            passwordHasher.Object
        );

        // Act
        Func<Task> acao = () =>
            servico.CriarAsync("Outro usuário", email, "123456");

        // Assert
        await acao.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Já existe um usuário com este e-mail.");

        repositorio.Verify(
            r => r.BuscarPorEmailAsync(email),
            Times.Once
        );

        repositorio.Verify(
            r => r.AdicionarAsync(It.IsAny<Usuario>()),
            Times.Never
        );

        passwordHasher.Verify(
            h => h.HashPassword(It.IsAny<Usuario>(), It.IsAny<string>()),
            Times.Never
        );
    }
}