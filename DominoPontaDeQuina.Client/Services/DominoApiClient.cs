using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DominoPontaDeQuina.Client.Services;

public sealed record ApiCallResult(HttpStatusCode StatusCode, string RawBody, JsonNode? Json, bool TransportError = false)
{
    public bool IsSuccess => !TransportError && (int)StatusCode is >= 200 and <= 299;
    public bool IsNoContent => !TransportError && StatusCode == HttpStatusCode.NoContent;
}

public sealed class DominoApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public async Task<ApiCallResult> SendAsync(
        HttpMethod method,
        string relativePath,
        string? bearerToken = null,
        string? activationKey = null,
        object? body = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, relativePath.TrimStart('/'));

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());
        }

        if (!string.IsNullOrWhiteSpace(activationKey))
        {
            request.Headers.TryAddWithoutValidation("X-Jogador-Ativacao", activationKey.Trim());
        }

        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var raw = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken);

            JsonNode? json = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try { json = JsonNode.Parse(raw); }
                catch (JsonException) { /* Mensagens em texto puro também são exibidas. */ }
            }

            return new ApiCallResult(response.StatusCode, raw, json);
        }
        catch (HttpRequestException ex)
        {
            return new ApiCallResult(HttpStatusCode.ServiceUnavailable,
                $"Não foi possível conectar à API: {ex.Message}", null, TransportError: true);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ApiCallResult(HttpStatusCode.ServiceUnavailable,
                "A solicitação excedeu o tempo limite ao aguardar a API.", null, TransportError: true);
        }
    }

    public static string FormatError(ApiCallResult result)
    {
        if (result.TransportError) return result.RawBody;
        var status = (int)result.StatusCode;
        var apiMessage = JsonTools.FindText(result.Json, "mensagem", "message", "detail", "title", "erro", "error");
        if (string.IsNullOrWhiteSpace(apiMessage))
            apiMessage = result.RawBody.Trim();

        var guidance = status switch
        {
            401 => "Token ausente, inválido ou revogado. Solicite um novo token e salve-o no cliente.",
            403 => "A conta ou o perfil não tem permissão para esta operação.",
            409 => "Conflito de versão ou perfil já ativo em outro cliente. Atualize o estado e confira a ativação.",
            422 => "A API rejeitou a ação. A jogada não deve ser considerada realizada; confira turno, encaixe e ação permitida.",
            429 => "Limite de solicitações atingido. Aguarde o intervalo indicado pela API antes de tentar novamente.",
            _ when status >= 500 => "A API retornou um erro de servidor. Atualize o estado antes de repetir a ação.",
            _ => string.Empty
        };

        var prefix = $"HTTP {status} ({result.StatusCode})";
        if (!string.IsNullOrWhiteSpace(apiMessage)) prefix += $": {apiMessage}";
        if (!string.IsNullOrWhiteSpace(guidance)) prefix += $" {guidance}";
        return prefix;
    }
}

public static class JsonTools
{
    public static JsonNode? FindValue(JsonNode? root, params string[] names)
    {
        if (root is JsonObject obj)
        {
            foreach (var name in names)
            {
                foreach (var property in obj)
                {
                    if (string.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase))
                        return property.Value;
                }
            }

            foreach (var property in obj)
            {
                var value = FindValue(property.Value, names);
                if (value is not null) return value;
            }
        }
        else if (root is JsonArray array)
        {
            foreach (var item in array)
            {
                var value = FindValue(item, names);
                if (value is not null) return value;
            }
        }

        return null;
    }

    public static string? FindText(JsonNode? root, params string[] names)
    {
        var value = FindValue(root, names);
        if (value is null) return null;
        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text)) return text;
        return value.ToJsonString().Trim('"');
    }

    public static long? FindLong(JsonNode? root, params string[] names)
    {
        var value = FindValue(root, names);
        if (value is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<long>(out var number)) return number;
            if (long.TryParse(jsonValue.ToString(), out number)) return number;
        }
        return null;
    }

    public static bool? FindBool(JsonNode? root, params string[] names)
    {
        var value = FindValue(root, names);
        if (value is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<bool>(out var boolean)) return boolean;
            if (bool.TryParse(jsonValue.ToString(), out boolean)) return boolean;
        }
        return null;
    }

    public static string Pretty(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try { return JsonNode.Parse(json)?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? json; }
        catch (JsonException) { return json; }
    }
}
