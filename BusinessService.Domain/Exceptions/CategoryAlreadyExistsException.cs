namespace BusinessService.Domain.Exceptions;

public class CategoryAlreadyExistsException : Exception
{
    public CategoryAlreadyExistsException()
        : base("Category already exists.") { }

    public CategoryAlreadyExistsException(string message)
        : base(message) { }

    public CategoryAlreadyExistsException(string message, Exception? innerException)
        : base(message, innerException) { }
}