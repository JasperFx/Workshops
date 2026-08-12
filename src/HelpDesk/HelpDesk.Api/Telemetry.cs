using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HelpDesk.Api;

public static class Telemetry
{
    public const string ServiceName = "HelpDesk.Api";

    #region sample_otel_wiring
    public static void AddHelpDeskTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ServiceName))

            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()

                // Wolverine's ActivitySource is just "Wolverine". Spans for
                // messages sent, received, executed, retried, dead-lettered.
                .AddSource("Wolverine")

                // Marten's is "Marten" -- connections, batches, event appends.
                .AddSource("Marten")

                .AddOtlpExporter())

            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()

                // NOT "Wolverine". The meter is named per service, so a plain
                // AddMeter("Wolverine") silently exports nothing at all. This
                // is the single easiest thing to get wrong here.
                .AddMeter($"Wolverine:{ServiceName}")

                .AddMeter("Marten")
                .AddOtlpExporter());
    }
    #endregion

}
