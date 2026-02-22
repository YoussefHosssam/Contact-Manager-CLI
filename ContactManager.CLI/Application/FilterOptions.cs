namespace ContactManager.Application;

public sealed record FilterOptions(
    DateTimeOffset? CreatedAfter = null,
    DateTimeOffset? CreatedBefore = null,
    string? EmailDomain = null,
    string? NameStartsWith = null
);