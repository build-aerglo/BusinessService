namespace BusinessService.Domain.Exceptions;

public class BusinessUserNotFoundException : Exception
{
    public BusinessUserNotFoundException()
        : base("Business user not found.") { }

    public BusinessUserNotFoundException(string message)
        : base(message) { }

    public BusinessUserNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class BusinessUserAlreadyExistsException : Exception
{
    public BusinessUserAlreadyExistsException()
        : base("A user with this email already exists for this business.") { }

    public BusinessUserAlreadyExistsException(string message)
        : base(message) { }

    public BusinessUserAlreadyExistsException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class UserLimitExceededException : Exception
{
    public int MaxUsers { get; }
    public int CurrentUsers { get; }

    public UserLimitExceededException(int maxUsers, int currentUsers)
        : base($"User limit exceeded. Current: {currentUsers}, Maximum: {maxUsers}")
    {
        MaxUsers = maxUsers;
        CurrentUsers = currentUsers;
    }

    public UserLimitExceededException(string message)
        : base(message)
    {
        MaxUsers = 0;
        CurrentUsers = 0;
    }
}

public class InvitationExpiredException : Exception
{
    public InvitationExpiredException()
        : base("The invitation has expired.") { }

    public InvitationExpiredException(string message)
        : base(message) { }
}

public class InvalidInvitationTokenException : Exception
{
    public InvalidInvitationTokenException()
        : base("Invalid invitation token.") { }

    public InvalidInvitationTokenException(string message)
        : base(message) { }
}

public class InsufficientPermissionException : Exception
{
    public string RequiredPermission { get; }

    public InsufficientPermissionException(string requiredPermission)
        : base($"Insufficient permissions. Required: {requiredPermission}")
    {
        RequiredPermission = requiredPermission;
    }

    public InsufficientPermissionException(string message, string requiredPermission)
        : base(message)
    {
        RequiredPermission = requiredPermission;
    }
}
