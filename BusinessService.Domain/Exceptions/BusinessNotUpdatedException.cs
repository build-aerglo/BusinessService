namespace BusinessService.Domain.Exceptions;

public class BusinessNotUpdatedException : Exception
{
    public BusinessNotUpdatedException()
        : base("Business not updated.") { }

    public BusinessNotUpdatedException(string message)
        : base(message) { }

    public BusinessNotUpdatedException(string message, Exception? innerException)
        : base(message, innerException) { }
}