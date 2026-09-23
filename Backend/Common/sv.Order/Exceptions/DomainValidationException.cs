namespace Sv.Order.Exceptions;

// Only deliberate business-rule failures may expose their messages to API clients.
public sealed class DomainValidationException(string message) : Exception(message);
