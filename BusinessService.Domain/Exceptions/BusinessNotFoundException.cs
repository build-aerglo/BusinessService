namespace BusinessService.Domain.Exceptions;

public class BusinessNotFoundException : Exception
{
    public BusinessNotFoundException()
        : base("Business not found.") { }

    public BusinessNotFoundException(string message)
        : base(message) { }

    public BusinessNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}