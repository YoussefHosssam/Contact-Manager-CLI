using ContactManager.Application;
using ContactManager.Infrastructure;
using ContactManager.Presentation;

var ct = CancellationToken.None;

// Store contacts.json beside the executable (simple + portable)
var dataPath = Path.Combine(AppContext.BaseDirectory, "contacts.json");
IContactRepository repo = new JsonContactRepository(dataPath);
IContactService service = new ContactService(repo);
var ui = new ConsoleUi(service);

await ui.RunAsync(ct);