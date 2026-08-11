// Launches the Help Desk API, the out-of-process notification service, and the
// CritterWatch console together.
//
// Infrastructure is NOT started here -- Postgres and Rabbit come from
// docker-compose.yml at the root of the repo, on ports 5440 and 5682. Bring
// those up first:
//
//     docker compose up -d
//
// Then:
//
//     dotnet run --project src/HelpDesk/HelpDesk.AppHost

var builder = DistributedApplication.CreateBuilder(args);

#region sample_apphost_fleet
var api = builder
    .AddProject<Projects.HelpDesk_Api>("helpdesk-api")
    .WithHttpEndpoint(port: 5199, name: "api");

var notifications = builder
    .AddProject<Projects.HelpDesk_Notifications>("notifications");

// Read-only monitoring needs no licence key. Administrative actions -- dead
// letter replay, pausing listeners, kicking off a projection rebuild -- do.
// Supply it out of band and never in source control:
//
//     export JASPERFX__LICENSEKEY="..."
var critterwatch = builder
    .AddProject<Projects.HelpDesk_CritterWatch>("critterwatch")
    .WithHttpEndpoint(port: 5200, name: "console")
    .WithExternalHttpEndpoints();
#endregion

builder.Build().Run();
