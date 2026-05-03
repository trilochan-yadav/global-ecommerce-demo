using Analytics.API.Models;

namespace Analytics.API.Business.Interfaces;

public interface IAnalyticsService
{
    Task<List<ClickEventDto>> GetClicksAsync();
    Task<List<ConversionStatDto>> GetConversionsAsync();
    Task LogClickAsync(LogClickRequest request);
    Task LogConversionAsync(LogConversionRequest request);
}
