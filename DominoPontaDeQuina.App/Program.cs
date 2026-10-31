using DominoPontaDeQuina.App;
using DominoPontaDeQuina.Repository.Context;
using DominoPontaDeQuina.Repository.Interfaces;
using DominoPontaDeQuina.Repository.Repositories;
using DominoPontaDeQuina.Repository.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDbContext<DominoDbContext>();

services.AddScoped<IUsuarioRepository, UsuarioRepository>();
services.AddScoped<IJogadorRepository, JogadorRepository>();
services.AddScoped<IPartidaRepository, PartidaRepository>();
services.AddScoped<
    IParticipacaoPartidaRepository,
    ParticipacaoPartidaRepository>();

services.AddScoped<UsuarioService>();
services.AddScoped<JogadorService>();
services.AddScoped<PartidaService>();
services.AddScoped<ParticipacaoPartidaService>();

services.AddScoped<App>();

using var provider = services.BuildServiceProvider();

var app = provider.GetRequiredService<App>();

await app.ExecutarAsync();