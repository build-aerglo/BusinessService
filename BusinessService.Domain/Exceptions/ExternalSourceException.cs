namespace BusinessService.Domain.Exceptions;

public class ExternalSourceNotFoundException : Exception
{
    public ExternalSourceNotFoundException()
        : base("External source not found.") { }

    public ExternalSourceNotFoundException(string message)
        : base(message) { }

    public ExternalSourceNotFoundException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class ExternalSourceAlreadyConnectedException : Exception
{
    public ExternalSourceAlreadyConnectedException()
        : base("This external source is already connected.") { }

    public ExternalSourceAlreadyConnectedException(string message)
        : base(message) { }

    public ExternalSourceAlreadyConnectedException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class ExternalSourceLimitExceededException : Exception
{
    public int MaxSources { get; }
    public int CurrentSources { get; }

    public ExternalSourceLimitExceededException(int maxSources, int currentSources)
        : base($"External source limit exceeded. Current: {currentSources}, Maximum: {maxSources}")
    {
        MaxSources = maxSources;
        CurrentSources = currentSources;
    }

    public ExternalSourceLimitExceededException(string message)
        : base(message)
    {
        MaxSources = 0;
        CurrentSources = 0;
    }
}

public class ExternalSourceSyncException : Exception
{
    public ExternalSourceSyncException()
        : base("Failed to sync external source.") { }

    public ExternalSourceSyncException(string message)
        : base(message) { }

    public ExternalSourceSyncException(string message, Exception? innerException)
        : base(message, innerException) { }
}

public class InvalidExternalSourceConfigurationException : Exception
{
    public InvalidExternalSourceConfigurationException()
        : base("Invalid external source configuration.") { }

    public InvalidExternalSourceConfigurationException(string message)
        : base(message) { }

    public InvalidExternalSourceConfigurationException(string message, Exception? innerException)
        : base(message, innerException) { }
}
