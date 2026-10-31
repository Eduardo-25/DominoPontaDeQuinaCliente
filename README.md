# Domino Ponta de Quina

## Integrantes

Eduardo da Silva Lima (RM: 554804)

Enzo Bonacasatta (RM: 555372)

Guilherme Ulacco (RM: 558418)

Matheus Hostim (RM: 556517)

Estevam Melo (RM: 555124)

## Projetos

- `DominoPontaDeQuina.Core`: regras e fluxo do jogo, incluindo peças, mãos dos jogadores, tabuleiro, rodadas, partidas e cálculo de pontuação.
- `DominoPontaDeQuina.Domain`: entidades e enums persistentes, como `Usuario`, `Jogador`, `Partida` e `ParticipacaoPartida`.
- `DominoPontaDeQuina.Repository`: `DominoDbContext`, mapeamentos Fluent API, interfaces e implementações dos repositórios com Entity Framework Core e LINQ.
- `DominoPontaDeQuina.Migrations`: projeto responsável pelas migrations e atualizações do banco de dados.
- `DominoPontaDeQuina.Application`: camada de serviços para usuários, jogadores, partidas, participações e autenticação.
- `DominoPontaDeQuina.Api`: Web API REST com controllers, injeção de dependência, Swagger e autenticação JWT.
- `DominoPontaDeQuina.Tests`: testes automatizados utilizando xUnit, FluentAssertions e Moq.
- `DominoPontaDeQuina.Client`: aplicação web ASP.NET Core Razor Pages (.NET 8) desenvolvida para o Checkpoint 6. Consome a API oficial do professor e disponibiliza interface para autenticação, gerenciamento de partidas e execução de jogadas.
- `DominoPontaDeQuina.Client.Tests`: projeto de testes automatizados do cliente, incluindo interpretação do estado da partida, privacidade da mão do jogador e decisões do jogador autônomo.

## Arquitetura

O projeto utiliza uma arquitetura em camadas para separar as responsabilidades:

- `Core`: regras de negócio e fluxo do jogo.
- `Domain`: entidades e enums do modelo persistente.
- `Repository`: acesso ao banco de dados e consultas LINQ.
- `Application`: serviços e operações da aplicação.
- `Api`: endpoints HTTP e autenticação.
- `Migrations`: versionamento do esquema do banco de dados.
- `Tests`: validação automatizada das regras e dos serviços.
- `Client`: aplicação web responsável pela interface do jogador e pela comunicação com a API oficial do dominó.
- `Client.Tests`: testes automatizados das funcionalidades do cliente.

A injeção de dependência conecta os serviços aos repositórios e ao contexto do Entity Framework Core.

## Modelo persistente

`Usuario` representa a conta do usuário e pode possuir vários perfis `Jogador`. `Jogador` representa um perfil de jogo vinculado a um usuário.

`Partida` representa uma partida armazenada para consulta de histórico. `ParticipacaoPartida` relaciona um jogador a uma partida e registra sua posição, pontuação e condição de vencedor.

Os relacionamentos são configurados com Fluent API no `DominoDbContext`. O banco de dados utilizado é SQLite, com o Entity Framework Core responsável pela persistência e pelas consultas LINQ.

A classe `Jogo`, presente no projeto Core, faz parte do fluxo de execução do jogo e é distinta da entidade persistente `Partida`, presente no projeto Domain.

## API REST própria da solução e autenticação(local)

A API disponibiliza endpoints para operações de usuários, jogadores, partidas e participações em partidas.

O Swagger permite consultar e executar as operações documentadas. A autenticação utiliza JWT (JSON Web Token), com validação de assinatura, emissor, público-alvo e validade do token.

O fluxo básico de autenticação consiste em cadastrar um usuário, realizar login com e-mail e senha, receber um token JWT e utilizá-lo no botão `Authorize` do Swagger para acessar os endpoints protegidos.

As senhas são armazenadas utilizando hash por meio de `IPasswordHasher<Usuario>`, em vez de serem persistidas em texto puro.

## Cliente Web

O projeto `DominoPontaDeQuina.Client` implementa um cliente web utilizando ASP.NET Core Razor Pages e .NET 8.

Diferentemente da API própria da solução, o cliente consome a API oficial, responsável pelas regras e pelo estado autoritativo das partidas.

Principais funcionalidades:

- Solicitação de token por e-mail e autenticação.
- Listagem, seleção e ativação de perfis de jogadores.
- Criação e ingresso em partidas.
- Inicialização e consulta do estado da partida.
- Visualização da mesa e da própria mão.
- Envio de jogadas, compras e passagens.
- Atualização automática do estado da partida.
- Consulta do resultado oficial.
- Jogador autônomo baseado em heurísticas locais.

**API oficial:**

https://domino-quina-api-b1-pf2261.azurewebsites.net/

**Swagger:**

https://domino-quina-api-b1-pf2261.azurewebsites.net/swagger

**Executar o cliente:**

```bash
dotnet run --project DominoPontaDeQuina.Client/DominoPontaDeQuina.Client.csproj
```
Após iniciar, acesse:

http://localhost:5187/

Para conhecer o fluxo completo de utilização, autenticação, ativação e testes, consulte o README do cliente web.

## Pré-requisitos

- .NET 8 SDK
- Ferramenta `dotnet-ef` compatível com Entity Framework Core 8

Para instalar a ferramenta EF Core:

```bash
dotnet tool install --global dotnet-ef --version "8.*"
```

Para verificar a versão instalada:

```bash
dotnet ef --version
```

## Restaurar e compilar

Execute os comandos a partir da pasta raiz da solução, onde está o arquivo `DominoPontaDeQuina.slnx`.

```bash
dotnet restore DominoPontaDeQuina.slnx
dotnet build DominoPontaDeQuina.slnx
```

## Migrations

Os comandos devem utilizar `DominoPontaDeQuina.Migrations` como projeto de migrations e startup project:

```bash
dotnet ef migrations add NomeDaMigration --project DominoPontaDeQuina.Migrations --startup-project DominoPontaDeQuina.Migrations

dotnet ef database update --project DominoPontaDeQuina.Migrations --startup-project DominoPontaDeQuina.Migrations
```

Para listar as migrations existentes:

```bash
dotnet ef migrations list --project DominoPontaDeQuina.Migrations --startup-project DominoPontaDeQuina.Migrations
```

Substitua `NomeDaMigration` por um nome que descreva a alteração realizada no modelo.

## Executar a API

Para iniciar a aplicação:

```bash
dotnet run --project DominoPontaDeQuina.Api/DominoPontaDeQuina.Api.csproj
```

Utilize o endereço informado no terminal para acessar a aplicação. Em ambiente de desenvolvimento, o Swagger fica disponível no caminho `/swagger`.

## Testes automatizados

O projeto utiliza as seguintes ferramentas:

- `xUnit`: organização e execução dos testes automatizados.
- `FluentAssertions`: verificações de retornos e exceções.
- `Moq`: simulação de dependências, como os repositórios.

Os testes de pontuação verificam que somas múltiplas de 5 geram `soma / 5` pontos, incluindo soma 10 com retorno 2 e soma 15 com retorno 3. Também são testadas somas que não são múltiplas de 5 e a exceção para somas negativas.

Os testes do `UsuarioService` verificam a consulta ao repositório antes do cadastro, a solicitação de salvamento de um usuário válido e o bloqueio de cadastros com e-mail duplicado.

Para executar somente os testes do projeto:

```bash
dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj --verbosity minimal
```

Para executar todos os testes da solução:

```bash
dotnet test DominoPontaDeQuina.slnx --verbosity minimal
```
Os testes automatizados do cliente estão no projeto
`DominoPontaDeQuina.Client.Tests`.

Para executá-los:

```bash
dotnet test DominoPontaDeQuina.Client.Tests/DominoPontaDeQuina.Client.Tests.csproj --verbosity minimal
```