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
            options.UseMongoDB(builder.Configuration.GetConnectionString("ShortenerURLContext") 
            ?? throw new InvalidOperationException("Connection string 'ShortenerURLContext' not found.")));


        //mongo-driver
        var mongoConnectionString = builder.Configuration.GetConnectionString("ShortenerURLContext")
            ?? throw new InvalidOperationException("MongoDB connection string not found.");

        var mongoUrl = MongoUrl.Create(mongoConnectionString);

        // Singleton: MongoClient is thread-safe and manages connection pooling internally.
        builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUrl));

        //Singleton because it is thread-safe, lightweight, and reuses the shared client.
        builder.Services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoUrl.DatabaseName);
        });

        builder.Services.AddSingleton<ISequenceGenerator, MongoSequenceGenerator>();
        builder.Services.AddSingleton<IShortCodeGenerator,ShortCodeGenerator>();
    }
}
