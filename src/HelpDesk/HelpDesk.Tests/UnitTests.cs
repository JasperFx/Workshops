using HelpDesk.Contracts;
using HelpDesk.Incidents;
using Shouldly;

namespace HelpDesk.Tests;

#region sample_pure_function_unit_tests
/// <summary>
/// No mocks. No container. No database. The handlers are pure functions of
/// their inputs, so testing them is just calling them.
/// </summary>
public class CategoriseIncidentTests
{
    private static Incident AnIncident(
        IncidentCategory? category = null,
        IncidentStatus status = IncidentStatus.Pending) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), status, [], category);

    [Fact]
    public void raises_categorised_event_when_the_category_changes()
    {
        var incident = AnIncident(IncidentCategory.Hardware);
        var user = new User(Guid.CreateVersion7());

        var (events, messages) = CategoriseIncidentEndpoint.Post(
            new CategoriseIncident { Category = IncidentCategory.Database, Version = 1 },
            incident,
            user);

        events.Single().ShouldBeOfType<IncidentCategorised>()
            .Category.ShouldBe(IncidentCategory.Database);

        messages.Single().ShouldBeOfType<TryAssignPriority>()
            .IncidentId.ShouldBe(incident.Id);
    }

    [Fact]
    public void does_nothing_when_the_category_is_unchanged()
    {
        var (events, messages) = CategoriseIncidentEndpoint.Post(
            new CategoriseIncident { Category = IncidentCategory.Database, Version = 1 },
            AnIncident(IncidentCategory.Database),
            new User(Guid.CreateVersion7()));

        events.ShouldBeEmpty();
        messages.ShouldBeEmpty();
    }

    [Fact]
    public void rejects_a_closed_incident()
    {
        var problem = CategoriseIncidentEndpoint.AssertNotClosed(
            AnIncident(status: IncidentStatus.Closed));

        problem.Status.ShouldBe(400);
    }
}
#endregion

public class TryAssignPriorityTests
{
    private static Incident AnIncident(IncidentCategory? category, IncidentPriority? priority = null) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), IncidentStatus.Pending, [], category, priority);

    [Fact]
    public void assigns_the_priority_from_the_customer_rules()
    {
        var incident = AnIncident(IncidentCategory.Database);
        var rules = new CustomerPriorityRules
        {
            Id = incident.CustomerId,
            Priorities = { [IncidentCategory.Database] = IncidentPriority.High }
        };

        var (events, messages) = TryAssignPriorityHandler.Handle(
            new TryAssignPriority(incident.Id, Guid.CreateVersion7()), incident, rules);

        events.Single().ShouldBeOfType<IncidentPrioritised>()
            .Priority.ShouldBe(IncidentPriority.High);

        // High is not Critical, so nobody gets paged.
        messages.ShouldBeEmpty();
    }

    [Fact]
    public void pages_somebody_for_a_critical_incident()
    {
        var incident = AnIncident(IncidentCategory.Database);
        var rules = new CustomerPriorityRules
        {
            Id = incident.CustomerId,
            Priorities = { [IncidentCategory.Database] = IncidentPriority.Critical }
        };

        var (_, messages) = TryAssignPriorityHandler.Handle(
            new TryAssignPriority(incident.Id, Guid.CreateVersion7()), incident, rules);

        messages.Single().ShouldBeOfType<NotificationRequested>()
            .Channel.ShouldBe(NotificationChannel.Pager);
    }

    [Fact]
    public void does_nothing_when_the_customer_has_no_rules()
    {
        var incident = AnIncident(IncidentCategory.Database);

        var (events, messages) = TryAssignPriorityHandler.Handle(
            new TryAssignPriority(incident.Id, Guid.CreateVersion7()), incident, rules: null);

        events.ShouldBeEmpty();
        messages.ShouldBeEmpty();
    }
}

public class IncidentAggregateTests
{
    [Fact]
    public void folds_events_into_current_state()
    {
        var incident = new Incident(
                Guid.CreateVersion7(), Guid.CreateVersion7(), IncidentStatus.Pending, [])
            .Apply(new IncidentCategorised(IncidentCategory.Network, Guid.CreateVersion7()))
            .Apply(new IncidentPrioritised(IncidentPriority.High, Guid.CreateVersion7()))
            .Apply(new IncidentResolved(ResolutionType.Permanent, Guid.CreateVersion7()));

        incident.Category.ShouldBe(IncidentCategory.Network);
        incident.Priority.ShouldBe(IncidentPriority.High);
        incident.Status.ShouldBe(IncidentStatus.Resolved);
    }
}
