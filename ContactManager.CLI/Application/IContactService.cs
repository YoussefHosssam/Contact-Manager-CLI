using ContactManager.Domain;

namespace ContactManager.Application;

public interface IContactService
{
    Contact Add(string name, string phone, string email);
    bool Edit(int id, string? name, string? phone, string? email, out string message);
    bool Delete(int id, out string message);
    Contact? GetById(int id);
    IReadOnlyList<Contact> ListAll();
    IReadOnlyList<Contact> Search(string query);
    IReadOnlyList<Contact> Filter(FilterOptions options);

    bool IsDirty { get; }
    Task LoadAsync(CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}