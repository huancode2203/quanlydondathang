namespace Sv.Order.Exceptions;

public sealed class DomainConflictException(string message) : Exception(message);
