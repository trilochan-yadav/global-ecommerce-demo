using Microsoft.EntityFrameworkCore;

namespace Payment.API.Data.Repositories;

public class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    private readonly AppDbContext _db = db;

    public Task<Models.Payment?> GetByIdAsync(int id) =>
        _db.Payments.FindAsync(id).AsTask();

    public Task<Models.Payment?> GetByOrderIdAsync(int orderId) =>
        _db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

    public async Task AddAsync(Models.Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
    }
}
