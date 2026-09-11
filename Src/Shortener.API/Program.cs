




using Shortener.API.Observability;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.AddApplicationServices();
builder.AddApplicationValidation();
builder.AddMongoDb();
builder.AddCache();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenTelemetry().WithMetrics(builder =>{
    builder.AddPrometheusExporter();
    builder.AddMeter([ShortDiagnostic.MeterName]);
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<ShortenerURLContext>();

    await context.Database.EnsureCreatedAsync();
}


    app.MapOpenApi();
    app.MapScalarApiReference();

app.UseExceptionHandler();
//app.UseHttpsRedirection();

var shortenerGroup = app.MapGroup("/api/v1/shortener")
    .WithTags("Shortener APIs");

shortenerGroup.MapShortenerEndpoints();

app.MapRedirectEndpoints();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.Run();

