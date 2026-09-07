namespace Shortener.API.Models;

public sealed class SequenceDocument
{
    [BsonId]
    public string Id { get; set; } = default!;

    public long Value { get; set; }
}
