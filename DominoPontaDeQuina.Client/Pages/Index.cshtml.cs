using System.Net;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using DominoPontaDeQuina.Client.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DominoPontaDeQuina.Client.Pages;

public class IndexModel(DominoApiClient api, ProtectedClientStore secrets) : PageModel
{
    private const string TokenKey = "cp6.token";
    private const string EmailKey = "cp6.email";
    private const string PlayerKey = "cp6.playerId";
    private const string ActivationKey = "cp6.activationKey";
    private const string ActivationPlayerKey = "cp6.activationPlayerId";
    private const string ProfileActiveKey = "cp6.profileActive";
    private const string GameKey = "cp6.gameId";
    private const string StateKey = "cp6.state";
    private const string PlayersKey = "cp6.players";
    private const string GamesKey = "cp6.games";
    private const string ResultKey = "cp6.result";
    private const string PendingIdempotencyKey = "cp6.pendingIdempotency";
    private const string PendingIntentTypeKey = "cp6.pendingActionType";
    private const string PendingIntentPayloadKey = "cp6.pendingActionPayload";

    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Token { get; set; } = string.Empty;
    [BindProperty] public string JogadorId { get; set; } = string.Empty;
    [BindProperty] public string PartidaId { get; set; } = string.Empty;
    [BindProperty] public int PontuacaoAlvo { get; set; } = 50;
    [BindProperty] public int ValorA { get; set; }
    [BindProperty] public int ValorB { get; set; }
    [BindProperty] public int Lado { get; set; }
    [BindProperty] public bool AutoAtualizar { get; set; }

    public string ApiBaseUrl { get; private set; } = string.Empty;
    public string Feedback { get; private set; } = string.Empty;
    public string ActivationStatus { get; private set; } = "Perfil ainda não ativado neste cliente.";
    public string AutoStatus { get; private set; } = "Modo autônomo desativado.";
    public string EstadoJsonPrivado { get; private set; } = string.Empty;
    public string JogadoresJson { get; private set; } = string.Empty;
    public string PartidasJson { get; private set; } = string.Empty;
    public string ResultadoJson { get; private set; } = string.Empty;
    public string LastCreatedGameInfo { get; private set; } = string.Empty;
    public GameStateSnapshot EstadoAtual { get; private set; } = new();
    public string VersaoEstado => EstadoAtual.Versao?.ToString() ?? "Ainda não reconhecida";
    public bool TemToken => !string.IsNullOrWhiteSpace(EffectiveToken);
    public bool TemPerfilAtivado => HttpContext.Session.GetString(ProfileActiveKey) == "true";
    public bool AutoJogador => HttpContext.Session.GetString("cp6.autoPlayer") == "true";

    private string EffectiveToken => !string.IsNullOrWhiteSpace(Token)
        ? Token.Trim()
        : HttpContext.Session.GetString(TokenKey) ?? string.Empty;

    private string EffectiveActivationKey => HttpContext.Session.GetString(ActivationKey) ?? string.Empty;

    public void OnGet()
    {
        LoadSession();
        Feedback = TempData["Feedback"]?.ToString() ?? string.Empty;
    }

    public async Task<IActionResult> OnPostSolicitarTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
            return RedirectWithMessage("Informe um endereço de e-mail válido.");

        HttpContext.Session.SetString(EmailKey, Email.Trim());
        PersistClientData();
        var result = await api.SendAsync(HttpMethod.Post, "api/identidade/token", body: new { email = Email.Trim() });

        if (result.StatusCode == HttpStatusCode.Accepted)
            return RedirectWithMessage("A API aceitou a solicitação (202 Accepted). Confira a caixa de entrada do e-mail informado; esta resposta não contém o token.");

        return Complete(result, "Solicitação de token aceita pela API.");
    }

    public IActionResult OnPostGuardarToken()
    {
        if (string.IsNullOrWhiteSpace(Token))
            return RedirectWithMessage("Cole o token recebido por e-mail.");

        HttpContext.Session.SetString(TokenKey, Token.Trim());
        PersistClientData();
        return RedirectWithMessage("Token guardado de forma protegida neste computador. A API continuará validando-o em cada chamada.");
    }

    public async Task<IActionResult> OnPostListarJogadoresAsync()
    {
        if (!RequireToken(out var redirect)) return redirect!;
        var result = await api.SendAsync(HttpMethod.Get, "api/jogadores", EffectiveToken);
        return Complete(result, "Perfis carregados.", PlayersKey);
    }

    public IActionResult OnPostSelecionarPerfil()
    {
        if (string.IsNullOrWhiteSpace(JogadorId))
            return RedirectWithMessage("Informe ou selecione o ID do perfil de jogador.");

        var currentPlayer = HttpContext.Session.GetString(PlayerKey);
        if (TemPerfilAtivado && !string.Equals(currentPlayer, JogadorId.Trim(), StringComparison.OrdinalIgnoreCase))
            return RedirectWithMessage("Libere o perfil atualmente ativo antes de selecionar outro perfil. Isso evita perder a chave de ativação usada pelo servidor.");

        HttpContext.Session.SetString(PlayerKey, JogadorId.Trim());
        PersistClientData();
        return RedirectWithMessage("Perfil selecionado. Ative-o neste cliente antes de usar as ações privadas da partida.");
    }

    public async Task<IActionResult> OnPostAtivarPerfilAsync()
    {
        SyncSelections();
        if (!RequireToken(out var redirect)) return redirect!;
        if (string.IsNullOrWhiteSpace(JogadorId))
            return RedirectWithMessage("Informe o ID do perfil antes de ativá-lo.");

        // Uma chave hexadecimal aleatória de 32 bytes contém 64 caracteres.
        // A mesma chave é mantida na sessão para permitir repetir a ativação neste cliente.
        var activation = EffectiveActivationKey;
        var keyPlayer = HttpContext.Session.GetString(ActivationPlayerKey);
        if (string.IsNullOrWhiteSpace(activation) ||
            !string.Equals(keyPlayer, JogadorId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            activation = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            HttpContext.Session.SetString(ActivationPlayerKey, JogadorId.Trim());
        }

        // Persistimos a chave antes da chamada: se a resposta se perder, a próxima
        // tentativa reutiliza a mesma chave e a API pode reconhecer a ativação existente.
        HttpContext.Session.SetString(ActivationKey, activation);
        PersistClientData();
        var result = await api.SendAsync(
            HttpMethod.Post,
            $"api/jogadores/{Uri.EscapeDataString(JogadorId.Trim())}/ativar-cliente",
            EffectiveToken,
            body: new { chaveAtivacao = activation });

        if (result.IsSuccess)
        {
            HttpContext.Session.SetString(ProfileActiveKey, "true");
            HttpContext.Session.SetString(PlayerKey, JogadorId.Trim());
            PersistClientData();
            return Complete(result, "A API confirmou a ativação deste perfil neste cliente.");
        }

        // Em rejeição explícita (4xx), a ativação não foi aceita: descarte a chave nova.
        // Em erro de transporte/servidor, preserve-a para permitir repetir a ativação
        // com a mesma chave caso a API tenha concluído a operação mas a resposta se perca.
        if (!result.TransportError && (int)result.StatusCode < 500)
        {
            HttpContext.Session.Remove(ActivationKey);
            HttpContext.Session.Remove(ActivationPlayerKey);
            HttpContext.Session.Remove(ProfileActiveKey);
            PersistClientData();
        }
        return Complete(result);
    }

    public async Task<IActionResult> OnPostDesativarPerfilAsync()
    {
        SyncSelections();
        if (!RequireToken(out var redirect)) return redirect!;
        if (string.IsNullOrWhiteSpace(JogadorId) || string.IsNullOrWhiteSpace(EffectiveActivationKey))
            return RedirectWithMessage("Não há perfil ativado nesta sessão para liberar.");

        var result = await api.SendAsync(
            HttpMethod.Delete,
            $"api/jogadores/{Uri.EscapeDataString(JogadorId.Trim())}/desativar-cliente",
            EffectiveToken,
            EffectiveActivationKey);

        if (result.IsSuccess)
        {
            HttpContext.Session.Remove(ActivationKey);
            HttpContext.Session.Remove(ActivationPlayerKey);
            HttpContext.Session.Remove(ProfileActiveKey);
            PersistClientData();
            return Complete(result, "A API confirmou a liberação do perfil.");
        }
        return Complete(result);
    }

    public async Task<IActionResult> OnPostListarPartidasAsync()
    {
        if (!RequireToken(out var redirect)) return redirect!;
        var result = await api.SendAsync(
            HttpMethod.Get,
            "api/partidas",
            EffectiveToken,
            EffectiveActivationKey);
        return Complete(result, "Lista de partidas atualizada.", GamesKey);
    }

    public async Task<IActionResult> OnPostCriarPartidaAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        if (PontuacaoAlvo < 1) return RedirectWithMessage("A pontuação-alvo deve ser maior que zero.");

        var result = await api.SendAsync(
            HttpMethod.Post,
            "api/partidas",
            EffectiveToken,
            EffectiveActivationKey,
            new { pontuacaoAlvo = PontuacaoAlvo, jogadorId = JogadorId.Trim() });

        if (result.IsSuccess && result.Json is not null)
        {
            var createdMatch = JsonTools.FindValue(result.Json, "partida");
            var createdId = GetTopLevelText(result.Json, "partidaId", "id")
                ?? GetTopLevelText(createdMatch, "partidaId", "id")
                ?? JsonTools.FindText(result.Json, "partidaId");
            if (!string.IsNullOrWhiteSpace(createdId))
            {
                HttpContext.Session.SetString(GameKey, createdId);
                HttpContext.Session.SetString("cp6.lastCreatedGameInfo", $"ID da partida criada: {createdId}");
                PartidaId = createdId;
                PersistClientData();
            }
            else
            {
                HttpContext.Session.SetString("cp6.lastCreatedGameInfo", "A API confirmou a criação, mas o cliente não reconheceu o nome do campo do ID na resposta. Consulte o Swagger e informe o ID da partida manualmente.");
            }
        }

        return Complete(result, "A API confirmou a solicitação de criação. Confira o ID retornado; se não for preenchido automaticamente, copie-o da resposta documentada pela API.");
    }

    public async Task<IActionResult> OnPostEntrarPartidaAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        if (string.IsNullOrWhiteSpace(PartidaId))
            return RedirectWithMessage("Informe o ID da partida que deseja ingressar.");

        var result = await api.SendAsync(
            HttpMethod.Post,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}/entrar",
            EffectiveToken,
            EffectiveActivationKey,
            new { jogadorId = JogadorId.Trim() });

        return await CompleteActionAndRefreshAsync(result, "A API confirmou o ingresso na partida.");
    }

    public async Task<IActionResult> OnPostIniciarPartidaAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        if (string.IsNullOrWhiteSpace(PartidaId))
            return RedirectWithMessage("Informe o ID da partida que deseja iniciar.");

        var result = await api.SendAsync(
            HttpMethod.Post,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}/iniciar",
            EffectiveToken,
            EffectiveActivationKey,
            new { jogadorId = JogadorId.Trim() });

        return await CompleteActionAndRefreshAsync(result, "A API confirmou o comando de início da partida.");
    }

    public async Task<IActionResult> OnPostAtualizarEstadoAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        if (string.IsNullOrWhiteSpace(PartidaId))
            return RedirectWithMessage("Informe o ID da partida.");

        var result = await api.SendAsync(
            HttpMethod.Get,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}?jogadorId={Uri.EscapeDataString(JogadorId.Trim())}",
            EffectiveToken,
            EffectiveActivationKey);

        return CompleteState(result, "Estado privado da partida atualizado.");
    }

    public async Task<IActionResult> OnPostConsultarAtualizacoesAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        if (string.IsNullOrWhiteSpace(PartidaId))
            return RedirectWithMessage("Informe o ID da partida.");

        var version = GameStateParserVersion();
        var result = await api.SendAsync(
            HttpMethod.Get,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}/atualizacoes?jogadorId={Uri.EscapeDataString(JogadorId.Trim())}&versaoConhecida={version}",
            EffectiveToken,
            EffectiveActivationKey);

        if (result.StatusCode == HttpStatusCode.NoContent)
        {
            if (HttpContext.Session.GetString("cp6.autoRefresh") == "true") return RedirectToPage();
            return RedirectWithMessage("HTTP 204 No Content: nenhuma atualização foi informada. O estado anterior foi mantido.");
        }

        return CompleteState(result, "Atualizações recebidas da API.");
    }

    public async Task<IActionResult> OnPostJogarAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        if (ValorA is < 0 or > 6 || ValorB is < 0 or > 6)
            return RedirectWithMessage("Os valores da peça devem estar entre 0 e 6.");
        if (Lado is not (0 or 1))
            return RedirectWithMessage("Escolha 0 para a ponta esquerda ou 1 para a ponta direita.");

        var version = RequireVersion(out var versionRedirect);
        if (!version.HasValue) return versionRedirect!;

        return await PlayAsync(ValorA, ValorB, Lado, version.Value, "A API confirmou a jogada.");
    }

    public async Task<IActionResult> OnPostComprarAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        var version = RequireVersion(out var versionRedirect);
        if (!version.HasValue) return versionRedirect!;

        if (!TryBuildBasicActionPayload("comprar", version.Value, out var payload, out var payloadError))
            return RedirectWithMessage(payloadError);
        var result = await api.SendAsync(
            HttpMethod.Post,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}/comprar",
            EffectiveToken,
            EffectiveActivationKey,
            payload);
        return await CompleteActionAndRefreshAsync(result, "A API confirmou a solicitação de compra.");
    }

    public async Task<IActionResult> OnPostPassarAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        var version = RequireVersion(out var versionRedirect);
        if (!version.HasValue) return versionRedirect!;

        if (!TryBuildBasicActionPayload("passar", version.Value, out var payload, out var payloadError))
            return RedirectWithMessage(payloadError);
        var result = await api.SendAsync(
            HttpMethod.Post,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}/passar",
            EffectiveToken,
            EffectiveActivationKey,
            payload);
        return await CompleteActionAndRefreshAsync(result, "A API confirmou a solicitação de passagem.");
    }

    public async Task<IActionResult> OnPostAcaoAutonomaAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        if (string.IsNullOrWhiteSpace(PartidaId))
            return RedirectWithMessage("Informe o ID da partida antes de iniciar o jogador autônomo.");
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString(PendingIntentTypeKey)))
        {
            HttpContext.Session.SetString("cp6.autoPlayer", "false");
            HttpContext.Session.SetString("cp6.autoStatus", "Modo autônomo pausado até que a ação pendente seja repetida com a mesma intenção.");
            return RedirectWithMessage("Existe uma ação pendente após erro ou perda de resposta. Repita o mesmo comando manual (mesma ação e, para jogada, mesma peça e lado) para reutilizar a chave de idempotência antes de iniciar novas ações.");
        }

        // Cada ciclo consulta novamente o estado oficial; o agente nunca decide com
        // base apenas em uma cópia antiga da mesa.
        var currentState = await api.SendAsync(
            HttpMethod.Get,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}?jogadorId={Uri.EscapeDataString(JogadorId.Trim())}",
            EffectiveToken,
            EffectiveActivationKey);
        if (!currentState.IsSuccess || string.IsNullOrWhiteSpace(currentState.RawBody))
        {
            if (AutoJogador) HttpContext.Session.SetString("cp6.autoPlayer", "false");
            return Complete(currentState);
        }

        SaveJsonIfPresent(StateKey, currentState.RawBody);
        var currentVersion = JsonTools.FindLong(currentState.Json, "versaoEstado", "versaoAtual", "versao");
        if (currentVersion.HasValue) HttpContext.Session.SetString("cp6.version", currentVersion.Value.ToString());

        var snapshot = GameStateSnapshot.Parse(currentState.RawBody, JogadorId);
        if (snapshot.Situacao.Contains("final", StringComparison.OrdinalIgnoreCase) ||
            snapshot.Situacao.Contains("encerr", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetString("cp6.autoPlayer", "false");
            HttpContext.Session.SetString("cp6.autoStatus", "A partida terminou conforme o estado informado pela API. Modo autônomo pausado.");
            return RedirectToPage();
        }

        var decision = DominoAutonomousPlayer.Decide(snapshot);
        if (decision.Type == "wait")
        {
            HttpContext.Session.SetString("cp6.autoStatus", decision.Message);
            if (AutoJogador) return RedirectToPage();
            return RedirectWithMessage($"Autônomo: {decision.Message}");
        }

        var version = RequireVersion(out var versionRedirect);
        if (!version.HasValue) return versionRedirect!;

        if (decision.Type == "play" && decision.Piece is not null && decision.Side is int side)
        {
            HttpContext.Session.SetString("cp6.autoStatus", decision.Message);
            return await PlayAsync(decision.Piece.ValorA, decision.Piece.ValorB, side, version.Value,
                $"O agente autônomo escolheu uma jogada. {decision.Message}");
        }

        if (decision.Type == "buy")
        {
            if (!TryBuildBasicActionPayload("comprar", version.Value, out var payload, out var payloadError))
                return RedirectWithMessage(payloadError);
            var buy = await api.SendAsync(HttpMethod.Post,
                $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}/comprar",
                EffectiveToken, EffectiveActivationKey, payload);
            return await CompleteActionAndRefreshAsync(buy, $"Ação autônoma de compra enviada. {decision.Message}");
        }

        if (decision.Type == "pass")
        {
            if (!TryBuildBasicActionPayload("passar", version.Value, out var payload, out var payloadError))
                return RedirectWithMessage(payloadError);
            var pass = await api.SendAsync(HttpMethod.Post,
                $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}/passar",
                EffectiveToken, EffectiveActivationKey, payload);
            return await CompleteActionAndRefreshAsync(pass, $"Ação autônoma de passagem enviada. {decision.Message}");
        }

        return RedirectWithMessage("O autônomo não identificou uma ação executável; nenhuma ação foi enviada.");
    }

    public async Task<IActionResult> OnPostConsultarResultadoAsync()
    {
        SyncSelections();
        if (!RequireActivatedProfile(out var redirect)) return redirect!;
        if (string.IsNullOrWhiteSpace(PartidaId)) return RedirectWithMessage("Informe o ID da partida.");

        var result = await api.SendAsync(
            HttpMethod.Get,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}/resultado?jogadorId={Uri.EscapeDataString(JogadorId.Trim())}",
            EffectiveToken,
            EffectiveActivationKey);
        return Complete(result, "Resultado consultado.", ResultKey);
    }

    public IActionResult OnPostAlternarJogadorAutonomo()
    {
        var wasEnabled = AutoJogador;
        var enabled = !wasEnabled;
        HttpContext.Session.SetString("cp6.autoPlayer", enabled ? "true" : "false");
        if (enabled) HttpContext.Session.SetString("cp6.autoRefresh", "false");
        HttpContext.Session.SetString("cp6.autoStatus", enabled
            ? "Modo autônomo ativo. A cada 5 segundos, o cliente consulta o estado e só envia uma ação quando conseguir reconhecer uma ação permitida."
            : "Modo autônomo pausado pelo usuário.");
        return RedirectWithMessage(enabled
            ? "Jogador autônomo ativado. Mantenha esta aba aberta; use o botão novamente para pausar."
            : "Jogador autônomo pausado.");
    }

    public IActionResult OnPostAlternarAtualizacao()
    {
        AutoAtualizar = HttpContext.Session.GetString("cp6.autoRefresh") != "true";
        HttpContext.Session.SetString("cp6.autoRefresh", AutoAtualizar ? "true" : "false");
        if (AutoAtualizar) HttpContext.Session.SetString("cp6.autoPlayer", "false");
        return RedirectWithMessage(AutoAtualizar
            ? "Atualização automática ativada (consulta a API a cada 5 segundos)."
            : "Atualização automática desativada.");
    }

    public IActionResult OnPostLimparSessao()
    {
        HttpContext.Session.Clear();
        secrets.Clear();
        return RedirectWithMessage("Sessão local e credenciais protegidas removidas deste computador. Isso não desativa automaticamente o perfil na API.");
    }

    private async Task<IActionResult> PlayAsync(int valueA, int valueB, int side, long version, string successMessage)
    {
        HttpContext.Session.SetString("cp6.valueA", valueA.ToString());
        HttpContext.Session.SetString("cp6.valueB", valueB.ToString());
        HttpContext.Session.SetString("cp6.side", side.ToString());

        if (!TryBuildPlayPayload(valueA, valueB, side, version, out var payload, out var payloadError))
            return RedirectWithMessage(payloadError);

        var result = await api.SendAsync(
            HttpMethod.Post,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}/jogar",
            EffectiveToken,
            EffectiveActivationKey,
            payload);

        return await CompleteActionAndRefreshAsync(result, successMessage);
    }

    private bool TryBuildBasicActionPayload(string actionType, long version, out JsonNode payload, out string error)
    {
        var pendingType = HttpContext.Session.GetString(PendingIntentTypeKey);
        if (!string.IsNullOrWhiteSpace(pendingType))
        {
            if (!string.Equals(pendingType, actionType, StringComparison.OrdinalIgnoreCase))
            {
                payload = null!;
                error = $"Existe uma ação pendente do tipo '{pendingType}'. Repita essa mesma ação antes de iniciar '{actionType}'.";
                return false;
            }
            var saved = HttpContext.Session.GetString(PendingIntentPayloadKey);
            if (!string.IsNullOrWhiteSpace(saved))
            {
                payload = JsonNode.Parse(saved)!;
                error = string.Empty;
                return true;
            }
        }

        var idempotency = Guid.NewGuid().ToString();
        var body = new JsonObject
        {
            ["jogadorId"] = JogadorId.Trim(),
            ["versaoEsperada"] = version,
            ["chaveIdempotencia"] = idempotency
        };
        HttpContext.Session.SetString(PendingIdempotencyKey, idempotency);
        HttpContext.Session.SetString(PendingIntentTypeKey, actionType);
        HttpContext.Session.SetString(PendingIntentPayloadKey, body.ToJsonString());
        payload = body;
        error = string.Empty;
        return true;
    }

    private bool TryBuildPlayPayload(int valueA, int valueB, int side, long version, out JsonNode payload, out string error)
    {
        var pendingType = HttpContext.Session.GetString(PendingIntentTypeKey);
        if (!string.IsNullOrWhiteSpace(pendingType))
        {
            if (!string.Equals(pendingType, "jogar", StringComparison.OrdinalIgnoreCase))
            {
                payload = null!;
                error = $"Existe uma ação pendente do tipo '{pendingType}'. Repita essa mesma ação antes de enviar uma jogada.";
                return false;
            }

            var savedText = HttpContext.Session.GetString(PendingIntentPayloadKey);
            if (!string.IsNullOrWhiteSpace(savedText) && JsonNode.Parse(savedText) is JsonObject savedObject)
            {
                var samePiece = ReadSavedInt(savedObject, "valorA") == valueA &&
                                ReadSavedInt(savedObject, "valorB") == valueB &&
                                ReadSavedInt(savedObject, "lado") == side;
                if (!samePiece)
                {
                    payload = null!;
                    error = "Há uma jogada pendente após falha de comunicação. Para reutilizar a chave de idempotência, repita exatamente os mesmos valores e o mesmo lado antes de tentar outra jogada.";
                    return false;
                }
                payload = savedObject;
                error = string.Empty;
                return true;
            }
        }

        var idempotency = Guid.NewGuid().ToString();
        var body = new JsonObject
        {
            ["jogadorId"] = JogadorId.Trim(),
            ["versaoEsperada"] = version,
            ["chaveIdempotencia"] = idempotency,
            ["valorA"] = valueA,
            ["valorB"] = valueB,
            ["lado"] = side
        };
        HttpContext.Session.SetString(PendingIdempotencyKey, idempotency);
        HttpContext.Session.SetString(PendingIntentTypeKey, "jogar");
        HttpContext.Session.SetString(PendingIntentPayloadKey, body.ToJsonString());
        payload = body;
        error = string.Empty;
        return true;
    }

    private static int ReadSavedInt(JsonObject payload, string key)
    {
        var value = payload.FirstOrDefault(pair => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
        return int.TryParse(value?.ToString(), out var result) ? result : int.MinValue;
    }

    private void ClearPendingIntent()
    {
        HttpContext.Session.Remove(PendingIdempotencyKey);
        HttpContext.Session.Remove(PendingIntentTypeKey);
        HttpContext.Session.Remove(PendingIntentPayloadKey);
    }

    private async Task<IActionResult> CompleteActionAndRefreshAsync(ApiCallResult result, string successMessage)
    {
        if (!result.IsSuccess)
        {
            // Uma resposta rejeitada não é uma jogada realizada. Pausamos o agente
            // automático para evitar repetir indefinidamente uma ação inválida.
            if (AutoJogador)
            {
                HttpContext.Session.SetString("cp6.autoPlayer", "false");
                HttpContext.Session.SetString("cp6.autoStatus", "Modo autônomo pausado porque a API rejeitou uma ação.");
            }
            // Uma resposta 4xx confirma a rejeição e encerra esta intenção. Em erros
            // 5xx, preservamos a chave para permitir repetir a mesma intenção idempotente.
            if ((int)result.StatusCode < 500)
                ClearPendingIntent();
            // Rejeições de versão exigem leitura do estado autoritativo antes de outra ação.
            var message = DominoApiClient.FormatError(result);
            if (result.StatusCode == HttpStatusCode.Conflict)
                await RefreshStateSilentlyAsync();
            return RedirectWithMessage(message);
        }

        if (!string.IsNullOrWhiteSpace(result.RawBody)) SaveJsonIfPresent(StateKey, result.RawBody);
        ClearPendingIntent();

        var followUp = await RefreshStateSilentlyAsync();
        if (followUp is not null && !followUp.IsSuccess && followUp.StatusCode != HttpStatusCode.NoContent)
            return RedirectWithMessage($"{successMessage} A operação foi confirmada, mas a consulta seguinte do estado falhou: {DominoApiClient.FormatError(followUp)}");

        return RedirectWithMessage(successMessage);
    }

    private IActionResult CompleteState(ApiCallResult result, string successMessage)
    {
        if (result.IsSuccess && !result.IsNoContent && !string.IsNullOrWhiteSpace(result.RawBody))
        {
            SaveJsonIfPresent(StateKey, result.RawBody);
            var version = JsonTools.FindLong(result.Json, "versaoEstado", "versaoAtual", "versao");
            if (version.HasValue) HttpContext.Session.SetString("cp6.version", version.Value.ToString());
        }
        return Complete(result, successMessage);
    }

    private async Task<ApiCallResult?> RefreshStateSilentlyAsync()
    {
        if (string.IsNullOrWhiteSpace(PartidaId) || string.IsNullOrWhiteSpace(JogadorId) ||
            string.IsNullOrWhiteSpace(EffectiveToken) || string.IsNullOrWhiteSpace(EffectiveActivationKey))
            return null;

        var result = await api.SendAsync(HttpMethod.Get,
            $"api/partidas/{Uri.EscapeDataString(PartidaId.Trim())}?jogadorId={Uri.EscapeDataString(JogadorId.Trim())}",
            EffectiveToken, EffectiveActivationKey);
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.RawBody))
        {
            SaveJsonIfPresent(StateKey, result.RawBody);
            var version = JsonTools.FindLong(result.Json, "versaoEstado", "versaoAtual", "versao");
            if (version.HasValue) HttpContext.Session.SetString("cp6.version", version.Value.ToString());
        }
        return result;
    }

    private IActionResult Complete(ApiCallResult result, string? successMessage = null, string? saveBodyKey = null)
    {
        if (result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(saveBodyKey)) SaveJsonIfPresent(saveBodyKey, result.RawBody);
            var status = (int)result.StatusCode;
            var detail = string.IsNullOrWhiteSpace(successMessage)
                ? $"A API respondeu HTTP {status} ({result.StatusCode})."
                : successMessage;
            TempData["Feedback"] = $"{detail} Confirmação HTTP: {status} {result.StatusCode}.";
        }
        else
        {
            TempData["Feedback"] = DominoApiClient.FormatError(result);
        }
        return RedirectToPage();
    }

    private IActionResult RedirectWithMessage(string message)
    {
        TempData["Feedback"] = message;
        return RedirectToPage();
    }

    private bool RequireToken(out IActionResult? redirect)
    {
        if (string.IsNullOrWhiteSpace(EffectiveToken))
        {
            redirect = RedirectWithMessage("Solicite um token por e-mail e salve-o antes de chamar uma rota protegida.");
            return false;
        }
        redirect = null;
        return true;
    }

    private bool RequireActivatedProfile(out IActionResult? redirect)
    {
        SyncSelections();
        if (!RequireToken(out redirect)) return false;
        if (string.IsNullOrWhiteSpace(JogadorId) || string.IsNullOrWhiteSpace(EffectiveActivationKey) || !TemPerfilAtivado)
        {
            redirect = RedirectWithMessage("Selecione e ative um perfil de jogador neste cliente antes de usar esta operação.");
            return false;
        }
        return true;
    }

    private long? RequireVersion(out IActionResult? redirect)
    {
        var version = GameStateParserVersion();
        if (string.IsNullOrWhiteSpace(PartidaId))
        {
            redirect = RedirectWithMessage("Informe o ID da partida.");
            return null;
        }
        if (version < 0)
        {
            redirect = RedirectWithMessage("Versão de estado não reconhecida. Consulte novamente o estado privado da partida antes de enviar ações.");
            return null;
        }
        redirect = null;
        return version;
    }

    private long GameStateParserVersion()
    {
        var raw = HttpContext.Session.GetString(StateKey);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var json = JsonNode.Parse(raw);
                var version = JsonTools.FindLong(json, "versaoEstado", "versaoAtual", "versao");
                if (version.HasValue) return version.Value;
            }
            catch { }
        }
        if (long.TryParse(HttpContext.Session.GetString("cp6.version"), out var saved)) return saved;
        return -1;
    }

    private void SyncSelections()
    {
        if (!string.IsNullOrWhiteSpace(Email)) HttpContext.Session.SetString(EmailKey, Email.Trim());
        if (!string.IsNullOrWhiteSpace(Token)) HttpContext.Session.SetString(TokenKey, Token.Trim());
        if (!string.IsNullOrWhiteSpace(JogadorId)) HttpContext.Session.SetString(PlayerKey, JogadorId.Trim());
        if (!string.IsNullOrWhiteSpace(PartidaId)) HttpContext.Session.SetString(GameKey, PartidaId.Trim());
        if (string.IsNullOrWhiteSpace(JogadorId)) JogadorId = HttpContext.Session.GetString(PlayerKey) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(PartidaId)) PartidaId = HttpContext.Session.GetString(GameKey) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Email)) Email = HttpContext.Session.GetString(EmailKey) ?? string.Empty;
        PersistClientData();
    }

    private void PersistClientData()
    {
        secrets.Save(new PersistedClientData(
            Email: HttpContext.Session.GetString(EmailKey),
            Token: HttpContext.Session.GetString(TokenKey),
            JogadorId: HttpContext.Session.GetString(PlayerKey),
            ActivationKey: HttpContext.Session.GetString(ActivationKey),
            ActivationPlayerId: HttpContext.Session.GetString(ActivationPlayerKey),
            ProfileActive: HttpContext.Session.GetString(ProfileActiveKey) == "true",
            PartidaId: HttpContext.Session.GetString(GameKey)));
    }

    private void SaveJsonIfPresent(string key, string body)
    {
        if (!string.IsNullOrWhiteSpace(body)) HttpContext.Session.SetString(key, body);
    }

    private static string? GetTopLevelText(JsonNode? json, params string[] names)
    {
        if (json is not JsonObject obj) return null;
        foreach (var name in names)
        {
            foreach (var property in obj)
            {
                if (string.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase) && property.Value is not null)
                    return property.Value.ToString();
            }
        }
        return null;
    }

    private void LoadSession()
    {
        var persisted = secrets.Read();
        RestoreSessionValue(EmailKey, persisted.Email);
        RestoreSessionValue(TokenKey, persisted.Token);
        RestoreSessionValue(PlayerKey, persisted.JogadorId);
        RestoreSessionValue(ActivationKey, persisted.ActivationKey);
        RestoreSessionValue(ActivationPlayerKey, persisted.ActivationPlayerId);
        RestoreSessionValue(GameKey, persisted.PartidaId);
        if (HttpContext.Session.GetString(ProfileActiveKey) is null && persisted.ProfileActive)
            HttpContext.Session.SetString(ProfileActiveKey, "true");

        Email = HttpContext.Session.GetString(EmailKey) ?? Email;
        Token = HttpContext.Session.GetString(TokenKey) ?? Token;
        JogadorId = HttpContext.Session.GetString(PlayerKey) ?? JogadorId;
        PartidaId = HttpContext.Session.GetString(GameKey) ?? PartidaId;
        ApiBaseUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()[
            "DominoApi:BaseUrl"] ?? "https://domino-quina-api-b1-pf2261.azurewebsites.net/";
        EstadoJsonPrivado = HttpContext.Session.GetString(StateKey) ?? string.Empty;
        JogadoresJson = JsonTools.Pretty(HttpContext.Session.GetString(PlayersKey));
        PartidasJson = JsonTools.Pretty(HttpContext.Session.GetString(GamesKey));
        ResultadoJson = JsonTools.Pretty(HttpContext.Session.GetString(ResultKey));
        LastCreatedGameInfo = HttpContext.Session.GetString("cp6.lastCreatedGameInfo") ?? string.Empty;
        AutoAtualizar = HttpContext.Session.GetString("cp6.autoRefresh") == "true";
        if (int.TryParse(HttpContext.Session.GetString("cp6.valueA"), out var valueA)) ValorA = valueA;
        if (int.TryParse(HttpContext.Session.GetString("cp6.valueB"), out var valueB)) ValorB = valueB;
        if (int.TryParse(HttpContext.Session.GetString("cp6.side"), out var side)) Lado = side;
        EstadoAtual = GameStateSnapshot.Parse(EstadoJsonPrivado, JogadorId);
        ActivationStatus = TemPerfilAtivado
            ? $"Perfil {JogadorId} ativado neste cliente. A chave não é exibida."
            : "Perfil ainda não ativado neste cliente.";
        AutoStatus = HttpContext.Session.GetString("cp6.autoStatus")
            ?? (AutoJogador ? "Modo autônomo ativo." : "Modo autônomo desativado.");
    }

    private void RestoreSessionValue(string key, string? value)
    {
        if (HttpContext.Session.GetString(key) is null && !string.IsNullOrWhiteSpace(value))
            HttpContext.Session.SetString(key, value);
    }

}
