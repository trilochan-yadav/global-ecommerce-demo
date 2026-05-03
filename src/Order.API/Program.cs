using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Order.API.Business;
using Order.API.Data;
using Order.API.Messages;
using Order.API.ServiceClient.Analytics;
using Order.API.ServiceClient.Bff;
using Order.API.ServiceClient.Payment;
using Order.API.ServiceClient.Product;
using Order.API.ServiceClient.Shipping;
using Serilog;
using Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order.API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

// ── EF Core + SQLite ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? "Data Source=Data/Orders.db"));

// ── Repositories + Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// ── Message Queue (singleton — shared with background service) ────────────────
builder.Services.AddSingleton<IMessageQueue, LocalMessageQueue>();

// ── Named HttpClients with API key headers ───────────────────────────────────
foreach (var (name, keyKey) in new[]
{
    ("ProductApi",   "Services:ProductApi:ApiKey"),
    ("PaymentApi",   "Services:PaymentApi:ApiKey"),
    ("ShippingApi",  "Services:ShippingApi:ApiKey"),
    ("AnalyticsApi", "Services:AnalyticsApi:ApiKey"),
    ("BffApi",       "Services:BffApi:ApiKey"),
})
{
    builder.Services.AddHttpClient(name, (sp, client) =>
    {
        client.DefaultRequestHeaders.Add("X-Api-Key", sp.GetRequiredService<IConfiguration>()[keyKey]);
    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
}

// ── Generated API client interfaces (registered with factory for baseUrl + named HttpClient) ─
builder.Services.AddScoped<IProductApiClient>(sp =>
    new ProductApiClient(
        sp.GetRequiredService<IConfiguration>()["Services:ProductApi:BaseUrl"]!,
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("ProductApi")));

builder.Services.AddScoped<IPaymentApiClient>(sp =>
    new PaymentApiClient(
        sp.GetRequiredService<IConfiguration>()["Services:PaymentApi:BaseUrl"]!,
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("PaymentApi")));

builder.Services.AddScoped<IShippingApiClient>(sp =>
    new ShippingApiClient(
        sp.GetRequiredService<IConfiguration>()["Services:ShippingApi:BaseUrl"]!,
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("ShippingApi")));

builder.Services.AddScoped<IAnalyticsApiClient>(sp =>
    new AnalyticsApiClient(
        sp.GetRequiredService<IConfiguration>()["Services:AnalyticsApi:BaseUrl"]!,
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("AnalyticsApi")));

builder.Services.AddScoped<IBffNotificationApiClient>(sp =>
    new BffNotificationApiClient(
        sp.GetRequiredService<IConfiguration>()["Services:BffApi:BaseUrl"]!,
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("BffApi")));

// ── Background saga processor ─────────────────────────────────────────────────
builder.Services.AddHostedService<MessageProcessorService>();

var app = builder.Build();

// ── Migrations (skipped during swagger.exe invocation) ───────────────────────
if (Environment.GetEnvironmentVariable("SWAGGER_GEN") != "true")
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();

app.Run();

