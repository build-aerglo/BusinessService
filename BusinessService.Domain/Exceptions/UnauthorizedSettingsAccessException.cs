namespace BusinessService.Domain.Exceptions;

public class UnauthorizedSettingsAccessException(string message) : Exception(message);