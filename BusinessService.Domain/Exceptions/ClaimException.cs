namespace BusinessService.Domain.Exceptions;

public class ClaimNotFoundException : Exception
{
    public ClaimNotFoundException()
        : base("Business claim request not found.") { }

    public ClaimNotFoundException(string message)
        : base(message) { }

    public ClaimNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class ClaimAlreadyExistsException : Exception
{
    public ClaimAlreadyExistsException()
        : base("A pending claim already exists for this business.") { }

    public ClaimAlreadyExistsException(string message)
        : base(message) { }

    public ClaimAlreadyExistsException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class BusinessAlreadyClaimedException : Exception
{
    public BusinessAlreadyClaimedException()
        : base("This business has already been claimed.") { }

    public BusinessAlreadyClaimedException(string message)
        : base(message) { }

    public BusinessAlreadyClaimedException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class InvalidClaimOperationException : Exception
{
    public InvalidClaimOperationException()
        : base("Invalid claim operation.") { }

    public InvalidClaimOperationException(string message)
        : base(message) { }

    public InvalidClaimOperationException(string message, Exception? innerException)
        : base(message, innerException) { }
}
