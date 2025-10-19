namespace BusinessService.Domain.Exceptions;

public class CategoryNotFoundException : Exception
{
    public CategoryNotFoundException()
        : base("Category not found.") { }

    public CategoryNotFoundException(string message)
        : base(message) { }

    public CategoryNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}