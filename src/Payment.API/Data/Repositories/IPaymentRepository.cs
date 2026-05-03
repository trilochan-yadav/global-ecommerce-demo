namespace Payment.API.Data.Repositories;

public interface IPaymentRepository
{
    Task<Models.Payment?> GetByIdAsync(int id);
    Task<Models.Payment?> GetByOrderIdAsync(int orderId);
    Task AddAsync(Models.Payment payment);
}
