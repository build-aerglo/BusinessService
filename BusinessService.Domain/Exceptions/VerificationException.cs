namespace BusinessService.Domain.Exceptions;

public class VerificationNotFoundException : Exception
{
    public VerificationNotFoundException()
        : base("Business verification record not found.") { }

    public VerificationNotFoundException(string message)
        : base(message) { }

    public VerificationNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class VerificationRequiredException : Exception
{
    public string RequirementType { get; }

    public VerificationRequiredException(string requirementType)
        : base($"Verification required: {requirementType}")
    {
        RequirementType = requirementType;
    }

    public VerificationRequiredException(string message, string requirementType)
        : base(message)
    {
        RequirementType = requirementType;
    }
}

public class ReverificationRequiredException : Exception
{
    public string Reason { get; }

    public ReverificationRequiredException(string reason)
        : base($"Re-verification required: {reason}")
    {
        Reason = reason;
    }
}

public class InvalidVerificationDocumentException : Exception
{
    public InvalidVerificationDocumentException()
        : base("Invalid verification document.") { }

    public InvalidVerificationDocumentException(string message)
        : base(message) { }

    public InvalidVerificationDocumentException(string message, Exception? innerException)
        : base(message, innerException) { }
}
