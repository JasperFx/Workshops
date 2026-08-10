using System.Text.Json.Serialization;
using FluentValidation;
using HelpDesk.Contracts;
using HelpDesk.Incidents;
using JasperFx;
using Marten;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Marten;

namespace HelpDesk.Api.Longhand;

public class CategoriseIncident
{
    [JsonPropertyName("Id"), Identity]
    public Guid Id { get; set; }
    
    [JsonPropertyName("Category")]
    public IncidentCategory Category { get; set; }
    
    [JsonPropertyName("Version")]
    // This is to communicate to the server that
    // this command was issued assuming that the 
    // incident is currently at this revision
    // number
    public int Version { get; set; }
    
    public string Description { get; set; }
    
    public class Validator : AbstractValidator<CategoriseIncident>
    {
        public Validator()
        {
            RuleFor(x => x.Version).GreaterThan(0);
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Description).NotEmpty();
        }
    }
}



public static class CategoriseIncidentHandler
{
    public static readonly Guid SystemId = Guid.NewGuid();
    
    [AggregateHandler]
    // The object? as return value will be interpreted
    // by Wolverine as appending one or zero events
    public static IncidentCategorised? Handle(
        CategoriseIncident command, 
        Incident existing)
    {
        if (existing.Category != command.Category)
        {
            return new IncidentCategorised(command.Category, SystemId) ;
            
        }

        return null;
    }
}

public class CategoriseIncidentController : ControllerBase
{
    [HttpPost("/incidents/categorise")]
    public async Task<IResult> Post(
        [FromBody] CategoriseIncident command, 
        [FromServices] IDocumentSession session)
    {
        if (command.Category == null)
        {
            return Results.BadRequest("You forgot the category");
        }
        
        // This assumes the command is starting from a known spot
        var incident = await session
            .Events.FetchForWriting<Incident>(command.Id, command.Version);
        

        if (incident.Aggregate == null)
        {
            return Results.NotFound();
        }

        if (incident.Aggregate.Category != command.Category)
        {
            incident.AppendOne(new IncidentCategorised(command.Category, currentUserId()));

            await session.SaveChangesAsync();
        }

        return Results.Ok();
    }
    
    private Guid currentUserId()
    {
        // let's say that we do something here that "finds" the
        // user id as a Guid from the ClaimsPrincipal
        var userIdClaim = User.FindFirst("user-id");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var id))
        {
            return id;
        }

        throw new UnauthorizedAccessException("No user");
    }
}
