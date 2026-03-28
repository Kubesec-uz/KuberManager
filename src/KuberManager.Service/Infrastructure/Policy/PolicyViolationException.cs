namespace KuberManager.Service.Infrastructure.Policy;

public sealed class PolicyViolationException : Exception
{
    public PolicyViolationException(string message) : base(message) { }
}
