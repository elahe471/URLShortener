namespace Shortener.API.Infrastructure.Extensions;

public static class MongoDependencyInjection
{
    public static void AddMongoApplicationServices(this IHostApplicationBuilder builder)
    {

        // EF Core is used for regular persistence operations on UrlTags,
        // while the native MongoDB Driver is used for the sequence collection
        // because atomic operations such as findOneAndUpdate with $inc and upsert
        // are better handled directly through MongoDB's native API.


        builder.Services.AddDbContext<ShortenerURLContext>(options =>
    options.UseMongoDB(
        builder.Configuration.GetConnectionString("ShortenerURLContext") ??
        throw new InvalidOperationException(
            "Connection string 'ShortenerURLContext' not found.")));


        //mongo-driver
        var mongoConnectionString =
builder.Configuration
    .GetConnectionString("ShortenerURLContext")
?? throw new InvalidOperationException(
    "MongoDB connection string not found.");

        var mongoUrl =
            new MongoUrl(mongoConnectionString);

        builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(mongoConnectionString));

        builder.Services.AddSingleton(sp =>
        {
            var client =
                sp.GetRequiredService<IMongoClient>();

            return client.GetDatabase(
                mongoUrl.DatabaseName);
        });

        builder.Services.AddSingleton<
            ISequenceGenerator,
            MongoSequenceGenerator>();

        builder.Services.AddSingleton<
            IShortCodeGenerator,
            ShortCodeGenerator>();
    }
}
