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

## Arquitetura

O projeto utiliza uma arquitetura em camadas para separar as responsabilidades:

- `Core`: regras de negócio e fluxo do jogo.
- `Domain`: entidades e enums do modelo persistente.
- `Repository`: acesso ao banco de dados e consultas LINQ.
- `Application`: serviços e operações da aplicação.
- `Api`: endpoints HTTP e autenticação.
- `Migrations`: versionamento do esquema do banco de dados.
- `Tests`: validação automatizada das regras e dos serviços.

A injeção de dependência conecta os serviços aos repositórios e ao contexto do Entity Framework Core.

## Modelo persistente

`Usuario` representa a conta do usuário e pode possuir vários perfis `Jogador`. `Jogador` representa um perfil de jogo vinculado a um usuário.

`Partida` representa uma partida armazenada para consulta de histórico. `ParticipacaoPartida` relaciona um jogador a uma partida e registra sua posição, pontuação e condição de vencedor.

Os relacionamentos são configurados com Fluent API no `DominoDbContext`. O banco de dados utilizado é SQLite, com o Entity Framework Core responsável pela persistência e pelas consultas LINQ.

A classe `Jogo`, presente no projeto Core, faz parte do fluxo de execução do jogo e é distinta da entidade persistente `Partida`, presente no projeto Domain.

## API REST e autenticação

A API disponibiliza endpoints para operações de usuários, jogadores, partidas e participações em partidas.

O Swagger permite consultar e executar as operações documentadas. A autenticação utiliza JWT (JSON Web Token), com validação de assinatura, emissor, público-alvo e validade do token.

O fluxo básico de autenticação consiste em cadastrar um usuário, realizar login com e-mail e senha, receber um token JWT e utilizá-lo no botão `Authorize` do Swagger para acessar os endpoints protegidos.

As senhas são armazenadas utilizando hash por meio de `IPasswordHasher<Usuario>`, em vez de serem persistidas em texto puro.

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

O banco SQLite local `domino.db` é mantido na raiz da solução e ignorado pelo Git.

## Executar a API

Para iniciar a aplicação:

```bash
dotnet run --project DominoPontaDeQuina.Api/DominoPontaDeQuina.Api.csproj
```

Utilize o endereço informado no terminal para acessar a aplicação. Em ambiente de desenvolvimento, o Swagger fica disponível no caminho `/swagger`.

As configurações de JWT estão no arquivo `appsettings.json` do projeto API. Em produção, a chave secreta deve ser armazenada de forma segura e não deve ser versionada no repositório.

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