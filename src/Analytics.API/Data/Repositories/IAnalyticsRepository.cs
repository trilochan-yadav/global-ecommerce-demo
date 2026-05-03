namespace Analytics.API.Data.Repositories;

public interface IAnalyticsRepository
{
    Task<List<Models.ClickEvent>> GetClicksAsync();
    Task<List<Models.ConversionStat>> GetConversionsAsync();
    Task AddClickAsync(Models.ClickEvent clickEvent);
    Task AddConversionAsync(Models.ConversionStat conversionStat);
}
