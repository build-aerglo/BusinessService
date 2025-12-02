namespace BusinessService.Domain.Exceptions;

public class UpdateBusinessFailedException : Exception
{
    public UpdateBusinessFailedException()
        : base("Update Business Failed") { }

    public UpdateBusinessFailedException(string message)
        : base(message) { }

    public UpdateBusinessFailedException(string message, Exception? innerException)
        : base(message, innerException) { }
}