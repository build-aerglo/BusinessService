namespace BusinessService.Domain.Exceptions;

public class PaymentInitiationException : Exception
{
    public PaymentInitiationException()
        : base("Payment initiation failed.") { }

    public PaymentInitiationException(string message)
        : base(message) { }

    public PaymentInitiationException(string message, Exception? innerException)
        : base(message, innerException) { }
}
