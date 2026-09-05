using FluentValidation;
using Shortener.API.Validators;

namespace Shortener.API.Infrastructure.Extensions
{
    public static class ValidationDependencyInjection
    {
        public static void AddApplicationValidation(
            this IHostApplicationBuilder builder)
        {
            builder.Services.AddValidatorsFromAssemblyContaining<
                ShortenUrlRequestValidator>();
        }
    }
}
