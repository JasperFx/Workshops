using HelpDesk.Contracts;
using JasperFx;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.RabbitMQ;

#region sample_notifications_host
var builder = Host.CreateApplicationBuilder(args);

builder.UseWolverine(opts =>
{
    opts.UseRabbitMq(factory =>
        {
            factory.HostName = "localhost";
            factory.Port = 5682;
        })
        .AutoProvision()

        // Wolverine will build the exchange, the queue, and the binding for us.
        // Useful in development; section 8 covers whether you want it in prod.
        .BindExchange("notifications").ToQueue("notifications", "notification_binding");

    opts.ListenToRabbitQueue("notifications");
});

return await builder.Build().RunJasperFxCommands(args);
#endregion

#region sample_notification_handler
// A plain static method. It has no idea the message crossed a process boundary,
// and this handler would be byte-for-byte identical if NotificationRequested
// were still being handled in-process by a local queue.
public static class NotificationRequestedHandler
{
    public static void Handle(NotificationRequested message)
    {
        Console.WriteLine(
            $"[{message.Channel}] to customer {message.CustomerId}: {message.Subject}");
        Console.WriteLine($"    {message.Body}");
    }
}
#endregion
