using System.Diagnostics.Metrics;

namespace Shortener.API.Observability;

public sealed class ShortDiagnostic
{
    public const string MeterName = "Shortener.API";

    public const string LinksCreatedMetricName =
        "shortener.links.created";

    public const string RedirectsMetricName =
        "shortener.redirects";

    public const string RedirectDurationMetricName =
        "shortener.redirect.duration";

    public const string CacheDatabaseFallbackMetricName =
        "shortener.cache.database_fallback";

    private readonly Counter<long> _linksCreatedCounter;
    private readonly Counter<long> _redirectCounter;
    private readonly Counter<long> _cacheDatabaseFallbackCounter;
    private readonly Histogram<double> _redirectDuration;

    public ShortDiagnostic(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _linksCreatedCounter =
            meter.CreateCounter<long>(
                LinksCreatedMetricName,
                unit: "{link}",
                description: "Number of successfully created short URLs.");

        _redirectCounter =
            meter.CreateCounter<long>(
                RedirectsMetricName,
                unit: "{redirect}",
                description: "Number of redirect requests by result.");

        _redirectDuration =
            meter.CreateHistogram<double>(
                RedirectDurationMetricName,
                unit: "s",
                description: "Time spent resolving redirect requests.");

        _cacheDatabaseFallbackCounter =
            meter.CreateCounter<long>(
                CacheDatabaseFallbackMetricName,
                unit: "{request}",
                description:
                "Number of cache lookups that required the underlying database.");
    }

    public void LinkCreated()
    {
        _linksCreatedCounter.Add(1);
    }

    public void RedirectCompleted(
        string result,
        double durationSeconds)
    {
        var resultTag =
            new KeyValuePair<string, object?>(
                "result",
                result);

        _redirectCounter.Add(
            1,
            resultTag);

        _redirectDuration.Record(
            durationSeconds,
            resultTag);
    }

    public void CacheDatabaseFallback()
    {
        _cacheDatabaseFallbackCounter.Add(1);
    }
}