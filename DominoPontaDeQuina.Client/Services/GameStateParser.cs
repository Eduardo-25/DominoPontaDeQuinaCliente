using System.Text.Json.Nodes;

namespace DominoPontaDeQuina.Client.Services;

public sealed record DominoPieceView(int ValorA, int ValorB)
{
    public int Soma => ValorA + ValorB;
    public string Label => $"[{ValorA}|{ValorB}]";
}

public sealed class GameStateSnapshot
{
    public List<DominoPieceView> MinhaMao { get; init; } = [];
    public List<DominoPieceView> Mesa { get; init; } = [];
    public int? PontaEsquerda { get; init; }
    public int? PontaDireita { get; init; }
    public string Situacao { get; init; } = "Aguardando estado da API";
    public string ProximaAcao { get; init; } = "Não informada pela API";
    public string Turno { get; init; } = "Não informado pela API";
    public string Placar { get; init; } = "Não informado pela API";
    public long? Versao { get; init; }
    public bool? MinhaVez { get; init; }
    public bool PodeComprar { get; init; }
    public bool PodePassar { get; init; }
    public string PedrasAdversario { get; init; } = "Não informado pela API";
    public DominoPieceView? AberturaObrigatoria { get; init; }
    public bool TemEstado { get; init; }

    public static GameStateSnapshot Parse(string? json, string? jogadorId)
    {
        if (string.IsNullOrWhiteSpace(json)) return new GameStateSnapshot();

        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch { return new GameStateSnapshot { Situacao = "A resposta da API não contém JSON reconhecível." }; }

        // A rota /api/partidas/{id}?jogadorId=... deve devolver a visão privada
        // do participante autenticado. Nunca renderizamos arrays genéricos de mãos
        // dos participantes: só procuramos propriedades que representem a mão própria.
        var handNames = new[]
        {
            "minhaMao", "minhasPecas", "pecasNaMao", "pedrasNaMao"
        };
        var boardNames = new[]
        {
            "pecasNaMesa", "pedrasNaMesa", "pecasTabuleiro", "pedrasJogadas",
            "pecasJogadas", "pedrasMesa", "pecasMesa"
        };

        // Primeiro procuramos somente o objeto que corresponde ao jogador ativo.
        // Só a partir dele aceitamos nomes genéricos como `mao` ou `pecas`.
        JsonNode? ownParticipant = string.IsNullOrWhiteSpace(jogadorId)
            ? null
            : FindObjectForPlayer(root, jogadorId);
        JsonNode? handNode = null;

        if (ownParticipant is not null && !ReferenceEquals(ownParticipant, root))
        {
            handNode = FindArrayByNames(ownParticipant,
                new[] { "minhaMao", "minhasPecas", "pecasNaMao", "pedrasNaMao", "mao", "pecas", "pedras" },
                acceptRootMatch: true);
        }

        // A rota de estado é privada e recebe jogadorId. Se a API já retornar o
        // próprio estado no nível superior, aceitamos somente nomes de mão própria
        // ou a propriedade `mao` direta; nunca buscamos `mao` de outro participante.
        handNode ??= FindDirectArrayByNames(root, handNames);
        if (handNode is null && root is JsonObject rootObject)
        {
            var directHand = rootObject.FirstOrDefault(pair => string.Equals(pair.Key, "mao", StringComparison.OrdinalIgnoreCase)).Value;
            if (directHand is JsonArray directHandArray) handNode = directHandArray;
        }
        var ownHand = ParsePieces(handNode);

        var boardNode = FindArrayByNames(root, boardNames, acceptRootMatch: false);
        if (boardNode is null)
        {
            var boardObject = FindObjectByNames(root, "tabuleiro", "mesa");
            boardNode = FindArrayByNames(boardObject, new[] { "pecas", "pedras", "pecasNaMesa", "pedrasNaMesa" }, acceptRootMatch: true);
        }
        var board = ParsePieces(boardNode);

        var left = ReadNullableInt(JsonTools.FindValue(root, "pontaEsquerda", "valorPontaEsquerda"));
        var right = ReadNullableInt(JsonTools.FindValue(root, "pontaDireita", "valorPontaDireita"));
        if (board.Count > 0)
        {
            left ??= board[0].ValorA;
            right ??= board[^1].ValorB;
        }

        var openingNode = JsonTools.FindValue(root, "pedraAberturaObrigatoria", "pecaAberturaObrigatoria", "pedraObrigatoria", "pecaObrigatoria");
        DominoPieceView? opening = TryPiece(openingNode, out var openingPiece) ? openingPiece : null;

        var situation = JsonTools.FindText(root, "situacao", "statusPartida", "status", "estadoPartida", "fase")
            ?? "Estado recebido da API";
        var nextAction = JsonTools.FindText(root, "proximaAcao", "acaoPermitida", "acaoEsperada", "proximaAcaoPermitida")
            ?? "Consultar o estado da API";
        var turn = JsonTools.FindText(root, "jogadorDaVez", "nomeJogadorAtual", "turnoAtual", "turno")
            ?? "Não informado pela API";
        var scoreNode = JsonTools.FindValue(root, "placar", "pontuacaoTimes", "pontuacaoPartida", "pontuacoes");
        var score = scoreNode is null ? "Não informado pela API" : SummarizeSafeScore(scoreNode);
        var version = JsonTools.FindLong(root, "versaoEstado", "versaoAtual", "versao");
        var myTurn = JsonTools.FindBool(root, "minhaVez", "souJogadorDaVez", "ehMinhaVez", "jogadorPodeJogar");
        if (myTurn is null)
        {
            if (ContainsAction(nextAction, "aguard") || ContainsAction(nextAction, "esper")) myTurn = false;
            else if (ContainsAction(nextAction, "jogar") || ContainsAction(nextAction, "compr") || ContainsAction(nextAction, "pass")) myTurn = true;
        }
        var canBuy = JsonTools.FindBool(root, "podeComprar", "compraPermitida", "podeComprarPedra") ?? ContainsAction(nextAction, "compr");
        var canPass = JsonTools.FindBool(root, "podePassar", "passagemPermitida", "podePassarVez") ?? ContainsAction(nextAction, "pass");
        var opponentCount = JsonTools.FindText(root, "quantidadePecasAdversario", "quantidadePedrasAdversario", "pedrasRestantesAdversario", "quantidadePecasOponente", "quantidadePedrasOponente");

        // Evita utilizar uma propriedade de outro participante caso a resposta inclua
        // dados agrupados por jogador. O jogadorId é mantido como contexto para logs/debug.
        _ = jogadorId;

        return new GameStateSnapshot
        {
            MinhaMao = ownHand,
            Mesa = board,
            PontaEsquerda = left,
            PontaDireita = right,
            Situacao = situation,
            ProximaAcao = nextAction,
            Turno = turn,
            Placar = score,
            Versao = version,
            MinhaVez = myTurn,
            PodeComprar = canBuy,
            PodePassar = canPass,
            PedrasAdversario = opponentCount ?? "Não informado pela API",
            AberturaObrigatoria = opening,
            TemEstado = true
        };
    }

    private static JsonNode? FindObjectForPlayer(JsonNode? node, string playerId)
    {
        if (node is JsonObject obj)
        {
            var idNode = obj.FirstOrDefault(p =>
                string.Equals(p.Key, "jogadorId", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Key, "idJogador", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Key, "id", StringComparison.OrdinalIgnoreCase)).Value;
            if (idNode is not null && string.Equals(idNode.ToString(), playerId, StringComparison.OrdinalIgnoreCase))
                return obj;
            foreach (var pair in obj)
            {
                var match = FindObjectForPlayer(pair.Value, playerId);
                if (match is not null) return match;
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                var match = FindObjectForPlayer(item, playerId);
                if (match is not null) return match;
            }
        }
        return null;
    }

    private static JsonNode? FindObjectByNames(JsonNode? node, params string[] names)
    {
        if (node is JsonObject obj)
        {
            foreach (var name in names)
                foreach (var pair in obj)
                    if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase) && pair.Value is JsonObject)
                        return pair.Value;
            foreach (var pair in obj)
            {
                var found = FindObjectByNames(pair.Value, names);
                if (found is not null) return found;
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                var found = FindObjectByNames(item, names);
                if (found is not null) return found;
            }
        }
        return null;
    }

    private static string SummarizeSafeScore(JsonNode node)
    {
        // O placar é informação pública; nunca retornamos JSON completo do estado,
        // pois ele pode conter informações privadas de participantes.
        if (node is JsonValue value) return value.ToString();
        if (node is JsonObject obj)
            return string.Join(" · ", obj.Select(pair => $"{pair.Key}: {pair.Value?.ToString() ?? "-"}"));
        if (node is JsonArray arr)
            return string.Join(" · ", arr.Select((item, index) => $"{index + 1}: {item?.ToString() ?? "-"}"));
        return "Pontuação recebida";
    }

    private static bool ContainsAction(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static int? ReadNullableInt(JsonNode? node)
    {
        if (node is null) return null;
        if (int.TryParse(node.ToString(), out var number)) return number;
        return null;
    }

    private static JsonNode? FindArrayByNames(JsonNode? root, string[] names, bool acceptRootMatch)
    {
        if (root is JsonObject rootObj)
        {
            foreach (var name in names)
            {
                foreach (var pair in rootObj)
                {
                    if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase) && pair.Value is JsonArray directArray &&
                        (acceptRootMatch || HasPieceCandidates(directArray)))
                        return directArray;
                }
            }
        }

        foreach (var name in names)
        {
            var found = FindNamedArray(root, name);
            if (found is not null && HasPieceCandidates(found)) return found;
        }
        return null;
    }

    private static JsonArray? FindDirectArrayByNames(JsonNode? root, string[] names)
    {
        if (root is not JsonObject obj) return null;
        foreach (var name in names)
            foreach (var pair in obj)
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase) && pair.Value is JsonArray array && HasPieceCandidates(array))
                    return array;
        return null;
    }

    private static JsonArray? FindNamedArray(JsonNode? node, string name)
    {
        if (node is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase) && pair.Value is JsonArray candidate)
                    return candidate;
            }
            foreach (var pair in obj)
            {
                var candidate = FindNamedArray(pair.Value, name);
                if (candidate is not null) return candidate;
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                var candidate = FindNamedArray(item, name);
                if (candidate is not null) return candidate;
            }
        }
        return null;
    }

    private static bool HasPieceCandidates(JsonArray array) => array.Any(item => TryPiece(item, out _));

    private static List<DominoPieceView> ParsePieces(JsonNode? node)
    {
        var result = new List<DominoPieceView>();
        if (node is not JsonArray array) return result;
        foreach (var item in array)
            if (TryPiece(item, out var piece)) result.Add(piece);
        return result;
    }

    private static bool TryPiece(JsonNode? node, out DominoPieceView piece)
    {
        piece = new DominoPieceView(0, 0);
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("peca", out var nested) && TryPiece(nested, out piece)) return true;
            if (obj.TryGetPropertyValue("pedra", out nested) && TryPiece(nested, out piece)) return true;

            var a = ReadFromObject(obj, "valorA", "ValorA", "valor1", "ladoA", "a");
            var b = ReadFromObject(obj, "valorB", "ValorB", "valor2", "ladoB", "b");
            if (a.HasValue && b.HasValue && a.Value is >= 0 and <= 6 && b.Value is >= 0 and <= 6)
            {
                piece = new DominoPieceView(a.Value, b.Value);
                return true;
            }
        }
        else if (node is JsonArray pair && pair.Count >= 2)
        {
            var a = ReadNullableInt(pair[0]);
            var b = ReadNullableInt(pair[1]);
            if (a.HasValue && b.HasValue && a.Value is >= 0 and <= 6 && b.Value is >= 0 and <= 6)
            {
                piece = new DominoPieceView(a.Value, b.Value);
                return true;
            }
        }
        return false;
    }

    private static int? ReadFromObject(JsonObject obj, params string[] names)
    {
        foreach (var name in names)
        {
            foreach (var pair in obj)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                    return ReadNullableInt(pair.Value);
            }
        }
        return null;
    }
}

public sealed record AutonomousDecision(string Type, DominoPieceView? Piece = null, int? Side = null, string Message = "");

public static class DominoAutonomousPlayer
{
    public static AutonomousDecision Decide(GameStateSnapshot state)
    {
        if (!state.TemEstado)
            return new("wait", Message: "Atualize o estado da partida antes de iniciar o modo autônomo.");
        if (state.MinhaVez != true)
            return new("wait", Message: "A resposta da API não confirma que este perfil pode agir neste momento. O autônomo está aguardando e não enviou ação.");

        if (state.MinhaMao.Count == 0)
            return new("wait", Message: "Não consegui identificar as peças da sua mão na resposta privada da API. Nenhuma ação foi enviada.");

        if (state.AberturaObrigatoria is not null)
        {
            var opening = state.MinhaMao.FirstOrDefault(p => SamePiece(p, state.AberturaObrigatoria));
            if (opening is not null)
                return new("play", opening, 0, "Jogando a peça obrigatória de abertura indicada pela API.");
            return new("wait", Message: "A API indicou uma peça obrigatória de abertura, mas ela não foi localizada na mão interpretada. Nenhuma ação foi enviada.");
        }

        if (!state.PontaEsquerda.HasValue || !state.PontaDireita.HasValue)
        {
            var first = state.MinhaMao.OrderByDescending(p => p.Soma).First();
            return new("play", first, 0, "Mesa sem pontas reconhecidas; tentando abrir com a peça de maior soma.");
        }

        var legalLeft = state.MinhaMao
            .Where(p => p.ValorA == state.PontaEsquerda || p.ValorB == state.PontaEsquerda)
            .OrderByDescending(p => p.Soma)
            .FirstOrDefault();
        if (legalLeft is not null)
            return new("play", legalLeft, 0, "O autônomo escolheu a jogada compatível de maior soma à esquerda.");

        var legalRight = state.MinhaMao
            .Where(p => p.ValorA == state.PontaDireita || p.ValorB == state.PontaDireita)
            .OrderByDescending(p => p.Soma)
            .FirstOrDefault();
        if (legalRight is not null)
            return new("play", legalRight, 1, "O autônomo escolheu a jogada compatível de maior soma à direita.");

        if (state.PodeComprar)
            return new("buy", Message: "Nenhuma peça encaixa; a API indica que comprar é permitido.");
        if (state.PodePassar)
            return new("pass", Message: "Nenhuma peça encaixa e a API indica que passar é permitido.");

        return new("wait", Message: "Nenhuma peça compatível foi reconhecida e a resposta não confirmou compra ou passagem. Nenhuma ação foi enviada.");
    }

    private static bool SamePiece(DominoPieceView a, DominoPieceView? b) =>
        b is not null && ((a.ValorA == b.ValorA && a.ValorB == b.ValorB) ||
                          (a.ValorA == b.ValorB && a.ValorB == b.ValorA));
}
