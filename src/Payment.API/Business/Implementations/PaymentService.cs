using Microsoft.Extensions.Logging;
using Payment.API.Business.Exceptions;
using Payment.API.Business.Interfaces;
using Payment.API.Data.Repositories;
using Payment.API.Models;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Shared;

namespace Payment.API.Business.Implementations;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IPaymentRepository repo, ILogger<PaymentService> logger)
    {
        _repo = repo;
        _logger = logger;

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<PaymentDeclinedException>(),
                MaxRetryAttempts = 1,
                Delay = TimeSpan.FromSeconds(5),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    logger.LogWarning("Retry {Attempt} for payment after {Delay}s due to: {Message}",
                        args.AttemptNumber, args.RetryDelay.TotalSeconds, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<PaymentDeclinedException>(),
                FailureRatio = 0.01,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .Build();
    }

    public async Task<PaymentDto> ProcessAsync(ProcessPaymentRequest request)
    {
        Data.Models.Payment? record = null;
        _logger.LogInformation("Processing payment for order {OrderId} with token {Token}", request.OrderId, request.PaymentToken?[..Math.Min(8, request.PaymentToken?.Length ?? 0)] + "...");

        try
        {
            await _pipeline.ExecuteAsync(async cancellationToken =>
            {
                // Simulate payment logic — token-based rules (swappable with real gateway)
                if (string.IsNullOrWhiteSpace(request.PaymentToken) ||
                    request.PaymentToken.StartsWith("tok_fail", StringComparison.OrdinalIgnoreCase))
                {
                    throw new PaymentDeclinedException($"Payment declined for token: {request.PaymentToken}");
                }

                // Success path — save record before returning
                record = new Data.Models.Payment
                {
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    Status = PaymentStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.AddAsync(record);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("Circuit breaker open — rejecting payment for order {OrderId}", request.OrderId);
            throw new InvalidOperationException("Payment circuit is open — too many recent failures. Try again later.");
        }
        catch (PaymentDeclinedException ex)
        {
            _logger.LogWarning("Payment declined for order {OrderId}: {Message}", request.OrderId, ex.Message);
            // Save failed record, then re-throw so controller returns 402
            var failed = new Data.Models.Payment
            {
                OrderId = request.OrderId,
                Amount = request.Amount,
                Status = PaymentStatus.Failed,
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddAsync(failed);
            throw;
        }

        _logger.LogInformation("Payment completed for order {OrderId}", request.OrderId);
        return ToDto(record!);
    }

    public async Task<PaymentDto?> GetByIdAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        return p == null ? null : ToDto(p);
    }

    private static PaymentDto ToDto(Data.Models.Payment p) => new()
    {
        Id = p.Id,
        OrderId = p.OrderId,
        Amount = p.Amount,
        Status = p.Status
    };
}
