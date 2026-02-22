using ContactManager.Application;
using ContactManager.Domain;

namespace ContactManager.Presentation;

public sealed class ConsoleUi
{
    private readonly IContactService _service;

    public ConsoleUi(IContactService service)
    {
        _service = service;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await _service.LoadAsync(ct);

        var existing = _service.ListAll();
        Console.WriteLine($"Loaded {existing.Count} contacts.");
        if (existing.Count > 0)
        {
            Console.WriteLine("Existing contacts:");
            PrintContacts(existing);
        }

        while (true)
        {
            PrintMenu();
            var choice = ConsoleInput.ReadInt("Select: ");

            Console.WriteLine();

            switch (choice)
            {
                case 1:
                    Add();
                    break;

                case 2:
                    Edit();
                    break;

                case 3:
                    Delete();
                    break;

                case 4:
                    View();
                    break;

                case 5:
                    ListAll();
                    break;

                case 6:
                    Search();
                    break;

                case 7:
                    Filter();
                    break;

                case 8:
                    await Save(ct);
                    break;

                case 9:
                    if (await Exit(ct)) return;
                    break;

                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private static void PrintMenu()
    {
        Console.WriteLine("==== Contact Manager ====");
        Console.WriteLine("1. Add Contact");
        Console.WriteLine("2. Edit Contact");
        Console.WriteLine("3. Delete Contact");
        Console.WriteLine("4. View Contact");
        Console.WriteLine("5. List Contacts");
        Console.WriteLine("6. Search");
        Console.WriteLine("7. Filter");
        Console.WriteLine("8. Save");
        Console.WriteLine("9. Exit");
        Console.WriteLine();
    }

    private void Add()
    {
        try
        {
            var name = ConsoleInput.ReadNonEmpty("Name: ");
            var phone = ConsoleInput.ReadNonEmpty("Phone: ");
            var email = ConsoleInput.ReadNonEmpty("Email: ");

            var c = _service.Add(name, phone, email);
            Console.WriteLine("Added:");
            Console.WriteLine(c);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void Edit()
    {
        var id = ConsoleInput.ReadInt("Id to edit: ");
        var existing = _service.GetById(id);
        if (existing is null)
        {
            Console.WriteLine("Not found.");
            return;
        }

        Console.WriteLine("Leave blank to keep current value.");
        Console.WriteLine($"Current: {existing}");

        var name = ConsoleInput.ReadOptional("New Name: ");
        var phone = ConsoleInput.ReadOptional("New Phone: ");
        var email = ConsoleInput.ReadOptional("New Email: ");

        if (_service.Edit(id, name, phone, email, out var msg))
            Console.WriteLine(msg);
        else
            Console.WriteLine($"Error: {msg}");
    }

    private void Delete()
    {
        var id = ConsoleInput.ReadInt("Id to delete: ");
        var existing = _service.GetById(id);
        if (existing is null)
        {
            Console.WriteLine("Not found.");
            return;
        }

        Console.WriteLine(existing);
        if (!ConsoleInput.ReadYesNo("Are you sure you want to delete?"))
        {
            Console.WriteLine("Cancelled.");
            return;
        }

        if (_service.Delete(id, out var msg))
            Console.WriteLine(msg);
        else
            Console.WriteLine($"Error: {msg}");
    }

    private void View()
    {
        var id = ConsoleInput.ReadInt("Id to view: ");
        var c = _service.GetById(id);
        if (c is null)
        {
            Console.WriteLine("Not found.");
            return;
        }

        Console.WriteLine(c);
    }

    private void ListAll()
    {
        var list = _service.ListAll();
        if (list.Count == 0)
        {
            Console.WriteLine("No contacts.");
            return;
        }
        PrintContacts(list);
    }

    private void Search()
    {
        var q = ConsoleInput.ReadNonEmpty("Search query (name/email/phone): ");
        var results = _service.Search(q);
        Console.WriteLine($"Found {results.Count} result(s).");
        PrintContacts(results);
    }

    private void Filter()
    {
        Console.WriteLine("Filter options (leave blank to skip):");

        DateTimeOffset? after = null;
        DateTimeOffset? before = null;

        var afterStr = ConsoleInput.ReadOptional("Created after (yyyy-MM-dd): ");
        if (!string.IsNullOrWhiteSpace(afterStr) &&
            DateTimeOffset.TryParse(afterStr, out var a))
            after = a;

        var beforeStr = ConsoleInput.ReadOptional("Created before (yyyy-MM-dd): ");
        if (!string.IsNullOrWhiteSpace(beforeStr) &&
            DateTimeOffset.TryParse(beforeStr, out var b))
            before = b;

        var domain = ConsoleInput.ReadOptional("Email domain (e.g. gmail.com): ");
        var starts = ConsoleInput.ReadOptional("Name starts with: ");

        var results = _service.Filter(new FilterOptions(after, before, domain, starts));
        Console.WriteLine($"Found {results.Count} result(s).");
        PrintContacts(results);
    }

    private async Task Save(CancellationToken ct)
    {
        await _service.SaveAsync(ct);
        Console.WriteLine("Saved.");
    }

    private async Task<bool> Exit(CancellationToken ct)
    {
        if (_service.IsDirty)
        {
            Console.WriteLine("You have unsaved changes.");
            if (ConsoleInput.ReadYesNo("Save before exit?"))
                await _service.SaveAsync(ct);
        }
        Console.WriteLine("Bye!");
        return true;
    }

    private static void PrintContacts(IReadOnlyList<Contact> contacts)
    {
        if (contacts.Count == 0)
        {
            Console.WriteLine("(empty)");
            return;
        }

        foreach (var c in contacts)
            Console.WriteLine(c);
    }
}