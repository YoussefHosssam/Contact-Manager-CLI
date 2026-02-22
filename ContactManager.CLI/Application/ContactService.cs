using ContactManager.Domain;
using ContactManager.Infrastructure;

namespace ContactManager.Application;

public sealed class ContactService : IContactService
{
    private readonly IContactRepository _repo;

    private readonly Dictionary<int, Contact> _byId = new();

    // token -> ids
    private readonly Dictionary<string, HashSet<int>> _nameIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<int>> _emailIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<int>> _phoneIndex = new(StringComparer.Ordinal);

    private int _nextId = 1;

    public bool IsDirty { get; private set; }

    public ContactService(IContactRepository repo)
    {
        _repo = repo;
    }

    public async Task LoadAsync(CancellationToken ct)
    {
        var loaded = await _repo.LoadAsync(ct);
        _byId.Clear();
        _nameIndex.Clear();
        _emailIndex.Clear();
        _phoneIndex.Clear();

        foreach (var c in loaded)
        {
            _byId[c.Id] = c;
            Index(c);
        }

        _nextId = (_byId.Count == 0) ? 1 : _byId.Keys.Max() + 1;
        IsDirty = false;
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        var list = _byId.Values
            .OrderBy(c => c.Id)
            .ToList();

        await _repo.SaveAsync(list, ct);
        IsDirty = false;
    }

    public Contact Add(string name, string phone, string email)
    {
        name = name.Trim();
        phone = phone.Trim();
        email = email.Trim();

        if (!ContactValidator.Validate(name, phone, email, out var err))
            throw new ArgumentException(err);

        var contact = new Contact(
            id: _nextId++,
            name: name,
            phone: phone,
            email: email,
            createdAt: DateTimeOffset.Now
        );

        _byId[contact.Id] = contact;
        Index(contact);

        IsDirty = true;
        return contact;
    }

    public bool Edit(int id, string? name, string? phone, string? email, out string message)
    {
        if (!_byId.TryGetValue(id, out var existing))
        {
            message = $"No contact with Id {id}.";
            return false;
        }

        var newName = (name ?? existing.Name).Trim();
        var newPhone = (phone ?? existing.Phone).Trim();
        var newEmail = (email ?? existing.Email).Trim();

        if (!ContactValidator.Validate(newName, newPhone, newEmail, out var err))
        {
            message = err;
            return false;
        }

        Unindex(existing);
        existing.Update(newName, newPhone, newEmail);
        Index(existing);

        IsDirty = true;
        message = "Contact updated.";
        return true;
    }

    public bool Delete(int id, out string message)
    {
        if (!_byId.TryGetValue(id, out var existing))
        {
            message = $"No contact with Id {id}.";
            return false;
        }

        Unindex(existing);
        _byId.Remove(id);

        IsDirty = true;
        message = "Contact deleted.";
        return true;
    }

    public Contact? GetById(int id)
        => _byId.TryGetValue(id, out var c) ? c : null;

    public IReadOnlyList<Contact> ListAll()
        => _byId.Values.OrderBy(c => c.Id).ToList();

    public IReadOnlyList<Contact> Search(string query)
    {
        query = query.Trim();
        if (query.Length == 0) return Array.Empty<Contact>();

        // Heuristics: email, phone, else name
        if (query.Contains('@'))
            return SearchByEmail(query);

        var digitCount = query.Count(char.IsDigit);
        if (digitCount >= Math.Max(4, query.Length / 2))
            return SearchByPhone(query);

        return SearchByName(query);
    }

    public IReadOnlyList<Contact> Filter(FilterOptions options)
    {
        IEnumerable<Contact> q = _byId.Values;

        if (options.CreatedAfter is not null)
            q = q.Where(c => c.CreatedAt > options.CreatedAfter);

        if (options.CreatedBefore is not null)
            q = q.Where(c => c.CreatedAt < options.CreatedBefore);

        if (!string.IsNullOrWhiteSpace(options.EmailDomain))
        {
            var dom = Normalize(options.EmailDomain);
            q = q.Where(c => GetEmailDomain(c.Email) == dom);
        }

        if (!string.IsNullOrWhiteSpace(options.NameStartsWith))
        {
            var pref = Normalize(options.NameStartsWith);
            q = q.Where(c => Normalize(c.Name).StartsWith(pref, StringComparison.Ordinal));
        }

        return q.OrderBy(c => c.Id).ToList();
    }

    // -------------------- indexing --------------------

    private static string Normalize(string s)
        => string.Join(' ', s.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static IEnumerable<string> NameTokens(string name)
        => Normalize(name).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static string PhoneKey(string phone)
        => ContactValidator.DigitsOnly(phone);

    private static string EmailKey(string email)
        => email.Trim().ToLowerInvariant();

    private static string GetEmailDomain(string email)
    {
        var e = EmailKey(email);
        var at = e.LastIndexOf('@');
        return (at >= 0 && at + 1 < e.Length) ? e[(at + 1)..] : "";
    }

    private void Index(Contact c)
    {
        foreach (var t in NameTokens(c.Name))
            AddIndex(_nameIndex, t, c.Id);

        AddIndex(_emailIndex, EmailKey(c.Email), c.Id);

        var pk = PhoneKey(c.Phone);
        if (pk.Length > 0)
            AddIndex(_phoneIndex, pk, c.Id);
    }

    private void Unindex(Contact c)
    {
        foreach (var t in NameTokens(c.Name))
            RemoveIndex(_nameIndex, t, c.Id);

        RemoveIndex(_emailIndex, EmailKey(c.Email), c.Id);

        var pk = PhoneKey(c.Phone);
        if (pk.Length > 0)
            RemoveIndex(_phoneIndex, pk, c.Id);
    }

    private static void AddIndex(Dictionary<string, HashSet<int>> index, string key, int id)
    {
        if (!index.TryGetValue(key, out var set))
        {
            set = new HashSet<int>();
            index[key] = set;
        }
        set.Add(id);
    }

    private static void RemoveIndex(Dictionary<string, HashSet<int>> index, string key, int id)
    {
        if (!index.TryGetValue(key, out var set)) return;
        set.Remove(id);
        if (set.Count == 0) index.Remove(key);
    }

    // -------------------- search impl --------------------

    private IReadOnlyList<Contact> SearchByEmail(string query)
    {
        var q = EmailKey(query);

        // exact match first
        if (_emailIndex.TryGetValue(q, out var exact))
            return exact.Select(id => _byId[id]).OrderBy(c => c.Id).ToList();

        // partial: contains
        var ids = new HashSet<int>();
        foreach (var (k, set) in _emailIndex)
        {
            if (k.Contains(q, StringComparison.Ordinal))
                ids.UnionWith(set);
        }
        return ids.Select(id => _byId[id]).OrderBy(c => c.Id).ToList();
    }

    private IReadOnlyList<Contact> SearchByPhone(string query)
    {
        var q = ContactValidator.DigitsOnly(query);

        // exact digits match
        if (_phoneIndex.TryGetValue(q, out var exact))
            return exact.Select(id => _byId[id]).OrderBy(c => c.Id).ToList();

        // partial: contains
        var ids = new HashSet<int>();
        foreach (var (k, set) in _phoneIndex)
        {
            if (k.Contains(q, StringComparison.Ordinal))
                ids.UnionWith(set);
        }
        return ids.Select(id => _byId[id]).OrderBy(c => c.Id).ToList();
    }

    private IReadOnlyList<Contact> SearchByName(string query)
    {
        var tokens = NameTokens(query).ToArray();
        if (tokens.Length == 0) return Array.Empty<Contact>();

        // Intersect token sets for AND-like search
        HashSet<int>? result = null;

        foreach (var t in tokens)
        {
            if (!_nameIndex.TryGetValue(t, out var set))
                return Array.Empty<Contact>();

            result = result is null ? new HashSet<int>(set) : IntersectInto(result, set);

            if (result.Count == 0)
                return Array.Empty<Contact>();
        }

        return result.Select(id => _byId[id]).OrderBy(c => c.Id).ToList();
    }

    private static HashSet<int> IntersectInto(HashSet<int> a, HashSet<int> b)
    {
        a.IntersectWith(b);
        return a;
    }
}