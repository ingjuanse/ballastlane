namespace FlowTrack.Domain.Exceptions;

public sealed class DomainRuleViolationException(string message) : InvalidOperationException(message)
{
}
