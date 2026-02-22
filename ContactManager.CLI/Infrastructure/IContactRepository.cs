using ContactManager.Domain;

namespace ContactManager.Infrastructure;

public interface IContactRepository
{
    Task<IReadOnlyList<Contact>> LoadAsync(CancellationToken ct);
    Task SaveAsync(IReadOnlyList<Contact> contacts, CancellationToken ct);
}