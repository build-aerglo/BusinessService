namespace BusinessService.Domain.Exceptions;

public class TagNotExistException : Exception
{
    public TagNotExistException()
        : base("Tag does not exist.") { }

    public TagNotExistException(string message)
        : base(message) { }

    public TagNotExistException(string message, Exception? innerException)
        : base(message, innerException) { }
}