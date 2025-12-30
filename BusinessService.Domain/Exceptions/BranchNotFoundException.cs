namespace BusinessService.Domain.Exceptions;

public class BranchNotFoundException : Exception
{
    public BranchNotFoundException()
        : base("Branch not found.") { }

    public BranchNotFoundException(string message)
        : base(message) { }

    public BranchNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}