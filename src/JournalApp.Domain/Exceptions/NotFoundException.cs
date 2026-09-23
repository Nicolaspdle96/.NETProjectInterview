namespace JournalApp.Domain.Exceptions;

public class NotFoundException(string message) : Exception(message);
