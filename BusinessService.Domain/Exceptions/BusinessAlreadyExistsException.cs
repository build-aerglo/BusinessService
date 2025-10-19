namespace BusinessService.Domain.Exceptions;

public class BusinessAlreadyExistsException : Exception
{
    public BusinessAlreadyExistsException() 
        : base("Business already exists.") { }

    public BusinessAlreadyExistsException(string message) 
        : base(message) { }

    public BusinessAlreadyExistsException(string message, Exception? innerException) 
        : base(message, innerException) { }
}
