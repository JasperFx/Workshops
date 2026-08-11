namespace HelpDesk.Scheduling;

#region sample_scheduling_exceptions
// Three failures that want three different responses. This is the whole point
// of section 3 -- a single global "retry three times" policy would treat all of
// these identically, and be wrong about two of them.

/// <summary>Transient. The technician's device dropped off the network; try again shortly.</summary>
public class TechnicianOfflineException(Guid technicianId)
    : Exception($"Technician {technicianId} is offline");

/// <summary>The whole downstream scheduling service is down. Retrying now is pointless.</summary>
public class SchedulingServiceDownException() : Exception("Scheduling service is unavailable");

/// <summary>This request can never succeed, no matter how many times we send it.</summary>
public class InvalidSchedulingRequestException(string reason) : Exception(reason);
#endregion

public enum FaultMode
{
    /// <summary>Everything works.</summary>
    None,

    /// <summary>Intermittent transient failures — retries should absorb these.</summary>
    Flaky,

    /// <summary>The service is hard down — this is what trips the circuit breaker.</summary>
    Down,

    /// <summary>Requests are rejected as invalid — these should be discarded, not retried.</summary>
    Invalid,

    /// <summary>The service accepts the call and then never returns — this is what timeouts are for.</summary>
    Hang
}

#region sample_fault_switch
/// <summary>
/// Lets the demo change how the downstream service misbehaves while the system
/// is running, so a room can watch retries absorb a blip, then watch the
/// circuit breaker trip when the same service goes hard down.
/// </summary>
public class FaultSwitch
{
    public FaultMode Mode { get; set; } = FaultMode.None;

    /// <summary>Percentage of calls that fail when Mode is Flaky.</summary>
    public int FlakyPercentage { get; set; } = 40;

    public int Attempts;
    public int Failures;
    public int Successes;
}
#endregion
