namespace Shortener.API.Services;

public sealed class MongoSequenceGenerator(
    IMongoDatabase database)
    : ISequenceGenerator
{
    private const string SequenceId = "short-url";

    private readonly IMongoCollection<SequenceDocument> _collection =
        database.GetCollection<SequenceDocument>("Sequences");

    public async Task<long> GetNextAsync(
        CancellationToken cancellationToken)
    {
        var filter =
            Builders<SequenceDocument>.Filter.Eq(
                x => x.Id,
                SequenceId);

        var update =
            Builders<SequenceDocument>.Update.Inc(
                x => x.Value,
                1);

        var options =
            new FindOneAndUpdateOptions<SequenceDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

        var sequence =
            await _collection.FindOneAndUpdateAsync(
                filter,
                update,
                options,
                cancellationToken);

        return sequence.Value;
    }
}