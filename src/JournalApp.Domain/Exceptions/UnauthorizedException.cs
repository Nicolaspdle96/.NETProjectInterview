namespace JournalApp.Domain.Exceptions;

public class UnauthorizedException(string message) : Exception(message);
