namespace BusinessService.Domain.Exceptions;

public class AutoResponseTemplateNotFoundException : Exception
{
    public AutoResponseTemplateNotFoundException()
        : base("Auto-response template not found.") { }

    public AutoResponseTemplateNotFoundException(string message)
        : base(message) { }

    public AutoResponseTemplateNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class AutoResponseTemplateAlreadyExistsException : Exception
{
    public AutoResponseTemplateAlreadyExistsException()
        : base("An auto-response template with this name already exists.") { }

    public AutoResponseTemplateAlreadyExistsException(string message)
        : base(message) { }

    public AutoResponseTemplateAlreadyExistsException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class AutoResponseDisabledException : Exception
{
    public AutoResponseDisabledException()
        : base("Auto-response feature is disabled for this business.") { }

    public AutoResponseDisabledException(string message)
        : base(message) { }
}

public class InvalidTemplateContentException : Exception
{
    public InvalidTemplateContentException()
        : base("Invalid template content.") { }

    public InvalidTemplateContentException(string message)
        : base(message) { }

    public InvalidTemplateContentException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class DndModeExtensionLimitException : Exception
{
    public int MaxExtensions { get; }
    public int CurrentExtensions { get; }

    public DndModeExtensionLimitException(int maxExtensions, int currentExtensions)
        : base($"DnD mode extension limit reached. Current: {currentExtensions}, Maximum: {maxExtensions}. Please contact support for further extensions.")
    {
        MaxExtensions = maxExtensions;
        CurrentExtensions = currentExtensions;
    }

    public DndModeExtensionLimitException(string message)
        : base(message)
    {
        MaxExtensions = 0;
        CurrentExtensions = 0;
    }
}
