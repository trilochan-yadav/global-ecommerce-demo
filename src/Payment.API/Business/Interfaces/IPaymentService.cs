using Payment.API.Models;

namespace Payment.API.Business.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto> ProcessAsync(ProcessPaymentRequest request);
    Task<PaymentDto?> GetByIdAsync(int id);
}
