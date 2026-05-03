using Analytics.API.Business.Interfaces;
using Analytics.API.Data.Repositories;
using Analytics.API.Models;

namespace Analytics.API.Business.Implementations;

public class AnalyticsService(IAnalyticsRepository repo) : IAnalyticsService
{
    private readonly IAnalyticsRepository _repo = repo;

    public async Task<List<ClickEventDto>> GetClicksAsync()
    {
        var events = await _repo.GetClicksAsync();
        return events.Select(e => new ClickEventDto
        {
            Id = e.Id,
            UserId = e.UserId,
            ProductId = e.ProductId,
            EventType = e.EventType,
            CreatedAt = e.CreatedAt
        }).ToList();
    }

    public async Task<List<ConversionStatDto>> GetConversionsAsync()
    {
        var stats = await _repo.GetConversionsAsync();
        return stats.Select(s => new ConversionStatDto
        {
            Id = s.Id,
            OrderId = s.OrderId,
            CustomerId = s.CustomerId,
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task LogClickAsync(LogClickRequest request)
    {
        await _repo.AddClickAsync(new Data.Models.ClickEvent
        {
            UserId = request.UserId,
            ProductId = request.ProductId,
            EventType = request.EventType,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task LogConversionAsync(LogConversionRequest request)
    {
        await _repo.AddConversionAsync(new Data.Models.ConversionStat
        {
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
