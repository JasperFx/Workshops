using CritterWatch.Services.Hosting;
using Wolverine.RabbitMQ;

// The CritterWatch console. A drop-in package on an otherwise empty ASP.NET
// Core site -- it is a separate application with its own hosting and its own
// security, not something bolted into the monitored service.

var builder = WebApplication.CreateBuilder(args);

#region sample_critterwatch_console
builder.AddCritterWatch(
    builder.Configuration.GetConnectionString("critterwatch")!,
    opts =>
    {
        var rabbit = builder.Configuration.GetSection("RabbitMq");
        opts.UseRabbitMq(factory =>
            {
                factory.HostName = rabbit["HostName"] ?? "localhost";
                factory.Port = int.Parse(rabbit["Port"] ?? "5672");
            })
            .AutoProvision();

        // CritterWatch reuses the transport the monitored services already
        // have. No extra infrastructure, no agent to install.
        opts.ListenToRabbitQueue("critterwatch");
    });
#endregion

var app = builder.Build();

app.UseCritterWatch();

await app.RunAsync();
