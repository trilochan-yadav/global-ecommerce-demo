using Microsoft.EntityFrameworkCore;

namespace Analytics.API.Data.Repositories;

public class AnalyticsRepository(AppDbContext db) : IAnalyticsRepository
{
    private readonly AppDbContext _db = db;

    public Task<List<Models.ClickEvent>> GetClicksAsync() =>
        _db.ClickEvents.OrderByDescending(e => e.CreatedAt).ToListAsync();

    public Task<List<Models.ConversionStat>> GetConversionsAsync() =>
        _db.ConversionStats.OrderByDescending(e => e.CreatedAt).ToListAsync();

    public async Task AddClickAsync(Models.ClickEvent clickEvent)
    {
        _db.ClickEvents.Add(clickEvent);
        await _db.SaveChangesAsync();
    }

    public async Task AddConversionAsync(Models.ConversionStat conversionStat)
    {
        _db.ConversionStats.Add(conversionStat);
        await _db.SaveChangesAsync();
    }
}
