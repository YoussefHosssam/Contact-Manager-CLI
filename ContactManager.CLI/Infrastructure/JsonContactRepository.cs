using System.Text.Json;
using ContactManager.Domain;

namespace ContactManager.Infrastructure;

public sealed class JsonContactRepository : IContactRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonContactRepository(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<Contact>> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return Array.Empty<Contact>();

        await using var fs = new FileStream(
            _filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        var contacts = await JsonSerializer.DeserializeAsync<List<Contact>>(fs);
        if (contacts== null)
        {
            return Array.Empty<Contact>();
        }
        return contacts;
    }

    public async Task SaveAsync(IReadOnlyList<Contact> contacts, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        // Write to temp then replace for safety
        var tempPath = _filePath + ".tmp";

        await using (var fs = new FileStream(
            tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 4096, useAsync: true))
        {
            await JsonSerializer.SerializeAsync(fs, contacts, _jsonOptions, ct);
        }

        File.Copy(tempPath, _filePath, overwrite: true);
        File.Delete(tempPath);
    }
}