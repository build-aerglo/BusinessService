namespace BusinessService.Domain.Exceptions;

public class TagNotFoundException : Exception
{
    public TagNotFoundException()
        : base("Tag does not exist.") { }

    public TagNotFoundException(string message)
        : base(message) { }

    public TagNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}