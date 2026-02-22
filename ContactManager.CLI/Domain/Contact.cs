using System.Text.Json.Serialization;

namespace ContactManager.Domain;

public sealed class Contact
{
    public int Id { get; init; }
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }

    // For JSON deserialization
    public Contact() { }

    [JsonConstructor]
    public Contact(int id, string name, string phone, string email, DateTimeOffset createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
        Update(name, phone, email);
    }

    public void Update(string name, string phone, string email)
    {
        Name = name;
        Phone = phone;
        Email = email;
    }

    public override string ToString()
        => $"[{Id}] {Name} | {Phone} | {Email} | Created: {CreatedAt:yyyy-MM-dd HH:mm:ss zzz}";
}