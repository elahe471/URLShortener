using Shortener.API.Endpoints;
using Shortener.API.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddApplicationValidation();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var shortenerGroup = app.MapGroup("/api/v1/shortener")
    .WithTags("Shortener APIs");

shortenerGroup.MapShortenerEndpoints();

app.Run();

