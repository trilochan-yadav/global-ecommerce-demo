namespace Payment.API.Business.Exceptions;

public class PaymentDeclinedException(string message) : Exception(message) { }
