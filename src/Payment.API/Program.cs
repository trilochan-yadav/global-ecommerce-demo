using Microsoft.EntityFrameworkCore;
using Payment.API.Business.Exceptions;
using Payment.API.Business.Implementations;
using Payment.API.Business.Interfaces;
using Payment.API.Data;
using Payment.API.Data.Repositories;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Serilog;
using Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Singleton so circuit breaker state is shared across all requests
builder.Services.AddSingleton<ResiliencePipeline>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PaymentService>>();
    return new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<PaymentDeclinedException>(),
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Constant,
            OnRetry = args =>
            {
                logger.LogWarning("Retry {Attempt} for payment after {Delay}s due to: {Message}",
                    args.AttemptNumber+1, args.RetryDelay.TotalSeconds, args.Outcome.Exception?.Message);
                return ValueTask.CompletedTask;
            }
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<PaymentDeclinedException>(),
            FailureRatio = 0.5,
            MinimumThroughput = 3,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .Build();
});

builder.Services.AddScoped<IPaymentService, PaymentService>();

var app = builder.Build();

if (Environment.GetEnvironmentVariable("SWAGGER_GEN") != "true")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandler(err => err.Run(async context =>
{
    context.Response.StatusCode = 500;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(ApiResponse<string>.Fail("An unexpected error occurred"));
}));

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();
app.Run();
