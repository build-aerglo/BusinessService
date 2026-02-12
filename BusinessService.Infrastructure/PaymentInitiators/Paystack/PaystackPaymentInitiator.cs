using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BusinessService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessService.Infrastructure.PaymentInitiators.Paystack;

public class PaystackPaymentInitiator : IPaymentInitiator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaystackPaymentInitiator> _logger;
    private readonly string _secretKey;
    private readonly string? _callbackUrl;

    public PaystackPaymentInitiator(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PaystackPaymentInitiator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _secretKey = configuration["Paystack:SecretKey"]
                     ?? throw new InvalidOperationException("Missing configuration: Paystack:SecretKey");
        _callbackUrl = configuration["Paystack:CallbackUrl"];
    }

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request)
    {
        _logger.LogInformation("Initiating Paystack payment for {Email}, amount {Amount}", request.Email, request.Amount);

        try
        {
            var paystackRequest = new
            {
                email = request.Email,
                amount = (int)(request.Amount * 100), // Paystack expects amount in kobo
                callback_url = request.CallbackUrl ?? _callbackUrl,
                metadata = request.Metadata != null
                    ? new { cancel_url = request.Metadata.GetValueOrDefault("cancel_url") }
                    : null
            };

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _secretKey);

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.paystack.co/transaction/initialize",
                paystackRequest);

            var result = await response.Content.ReadFromJsonAsync<PaystackInitializeResponse>();

            if (result is { Status: true })
            {
                _logger.LogInformation("Paystack payment initiated successfully. Reference: {Reference}", result.Data.Reference);
                return new PaymentInitiationResult(
                    Success: true,
                    Reference: result.Data.Reference,
                    PaymentUrl: result.Data.AuthorizationUrl,
                    Error: null
                );
            }

            _logger.LogWarning("Paystack payment initiation failed: {Message}", result?.Message);
            return new PaymentInitiationResult(
                Success: false,
                Reference: null,
                PaymentUrl: null,
                Error: result?.Message ?? "Transaction Error"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Paystack payment for {Email}", request.Email);
            return new PaymentInitiationResult(
                Success: false,
                Reference: null,
                PaymentUrl: null,
                Error: ex.Message
            );
        }
    }
}

internal class PaystackInitializeResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;

    [JsonPropertyName("data")]
    public PaystackInitializeData Data { get; set; } = default!;
}

internal class PaystackInitializeData
{
    [JsonPropertyName("authorization_url")]
    public string AuthorizationUrl { get; set; } = default!;

    [JsonPropertyName("access_code")]
    public string AccessCode { get; set; } = default!;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = default!;
}
