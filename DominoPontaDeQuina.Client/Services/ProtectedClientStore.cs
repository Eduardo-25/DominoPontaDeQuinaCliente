using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace DominoPontaDeQuina.Client.Services;

public sealed record PersistedClientData(
    string? Email = null,
    string? Token = null,
    string? JogadorId = null,
    string? ActivationKey = null,
    string? ActivationPlayerId = null,
    bool ProfileActive = false,
    string? PartidaId = null);

public static class ClientDataPaths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FIAP",
        "DominoPontaDeQuina.Client");
}

/// <summary>
/// Persiste token e chave de ativação cifrados para que o cliente possa ser reaberto.
/// No Windows, a chave do ASP.NET Data Protection é protegida por DPAPI para a conta
/// Windows atual. O cookie da sessão não contém esses segredos.
/// </summary>
public sealed class ProtectedClientStore
{
    private readonly string _filePath;
    private readonly IDataProtector _protector;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ProtectedClientStore(IDataProtectionProvider provider)
    {
        Directory.CreateDirectory(ClientDataPaths.Root);
        _filePath = Path.Combine(ClientDataPaths.Root, "client-secrets.bin");
        _protector = provider.CreateProtector("DominoPontaDeQuina.Client.Secrets.v1");
    }

    public PersistedClientData Read()
    {
        try
        {
            if (!File.Exists(_filePath)) return new PersistedClientData();
            var protectedValue = File.ReadAllText(_filePath, Encoding.UTF8);
            var json = _protector.Unprotect(protectedValue);
            return JsonSerializer.Deserialize<PersistedClientData>(json, JsonOptions)
                ?? new PersistedClientData();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        {
            // Uma sessão persistida que não possa ser descriptografada não é reutilizada.
            return new PersistedClientData();
        }
    }

    public void Save(PersistedClientData data)
    {
        Directory.CreateDirectory(ClientDataPaths.Root);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var protectedValue = _protector.Protect(json);
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, protectedValue, Encoding.UTF8);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    public void Clear()
    {
        if (File.Exists(_filePath)) File.Delete(_filePath);
    }
}
