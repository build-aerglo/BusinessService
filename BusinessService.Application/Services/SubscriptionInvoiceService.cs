using BusinessService.Application.DTOs.Subscription;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessService.Application.Services;

public class SubscriptionInvoiceService : ISubscriptionInvoiceService
{
    private readonly ISubscriptionInvoiceRepository _invoiceRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IPaymentInitiator _paymentInitiator;
    private readonly INotificationServiceClient _notificationClient;
    private readonly ILogger<SubscriptionInvoiceService> _logger;
    private readonly string _dateFormat;
    private readonly string _chargesDescription;
    private readonly decimal _chargesPercentage;
    private readonly decimal _chargesCap;

    public SubscriptionInvoiceService(
        ISubscriptionInvoiceRepository invoiceRepository,
        ISubscriptionPlanRepository planRepository,
        IBusinessRepository businessRepository,
        IPaymentInitiator paymentInitiator,
        INotificationServiceClient notificationClient,
        IConfiguration configuration,
        ILogger<SubscriptionInvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _planRepository = planRepository;
        _businessRepository = businessRepository;
        _paymentInitiator = paymentInitiator;
        _notificationClient = notificationClient;
        _logger = logger;
        _dateFormat = configuration["Invoice:DateFormat"] ?? "dd/MM/yyyy";
        _chargesDescription = configuration["Invoice:ChargesDescription"] ?? "Paystack Transaction Fee - (1.5%)";
        _chargesPercentage = decimal.TryParse(configuration["Invoice:ChargesPercentage"], out var pct) ? pct : 1.5m;
        _chargesCap = decimal.TryParse(configuration["Invoice:ChargesCap"], out var cap) ? cap : 2000m;
    }

    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request)
    {
        var business = await _businessRepository.FindByIdAsync(request.BusinessId)
                       ?? throw new BusinessNotFoundException($"Business {request.BusinessId} not found.");

        var plan = await _planRepository.FindByIdAsync(request.SubscriptionId)
                   ?? throw new SubscriptionNotFoundException($"Subscription plan {request.SubscriptionId} not found.");

        var platform = request.Platform ?? "paystack";
        var amount = request.IsAnnual ? plan.AnnualPrice : plan.MonthlyPrice;

        // Initiate payment
        var paymentResult = await _paymentInitiator.InitiatePaymentAsync(new PaymentInitiationRequest
        {
            Email = request.Email,
            Amount = amount
        });

        if (!paymentResult.Success)
        {
            throw new PaymentInitiationException(paymentResult.Error ?? "Payment initiation failed");
        }

        // Insert subscription invoice
        var invoice = new SubscriptionInvoice
        {
            Id = Guid.NewGuid(),
            IsAnnual = request.IsAnnual,
            BusinessId = request.BusinessId,
            Platform = platform,
            SubscriptionId = request.SubscriptionId,
            PaymentUrl = paymentResult.PaymentUrl,
            Email = request.Email,
            Reference = paymentResult.Reference,
            Status = "unpaid",
            CreatedAt = DateTime.UtcNow
        };

        await _invoiceRepository.AddAsync(invoice);

        // Send invoice notification (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await SendInvoiceNotificationAsync(invoice, plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invoice notification for invoice {InvoiceId}", invoice.Id);
            }
        });

        return new CheckoutResponse(
            Message: "Transaction initiated",
            Invoice: invoice.Id
        );
    }

    public async Task<SubscriptionInvoiceDto?> GetInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _invoiceRepository.FindByIdAsync(invoiceId);
        if (invoice == null) return null;

        var plan = await _planRepository.FindByIdAsync(invoice.SubscriptionId);

        SubscriptionPlanSummaryDto? planSummary = null;
        if (plan != null)
        {
            planSummary = new SubscriptionPlanSummaryDto(
                Id: plan.Id,
                Name: plan.Name,
                Tier: plan.Tier.ToString(),
                Description: plan.Description,
                MonthlyPrice: plan.MonthlyPrice,
                AnnualPrice: plan.AnnualPrice
            );
        }

        return new SubscriptionInvoiceDto(
            Id: invoice.Id,
            IsAnnual: invoice.IsAnnual,
            BusinessId: invoice.BusinessId,
            Platform: invoice.Platform,
            SubscriptionId: invoice.SubscriptionId,
            PaymentUrl: invoice.PaymentUrl,
            Email: invoice.Email,
            Reference: invoice.Reference,
            CreatedAt: invoice.CreatedAt,
            Status: invoice.Status,
            Subscription: planSummary
        );
    }

    private async Task SendInvoiceNotificationAsync(SubscriptionInvoice invoice, SubscriptionPlan plan)
    {
        var now = DateTime.UtcNow;
        var endDate = invoice.IsAnnual ? now.AddYears(1) : now.AddMonths(1);
        var amount = invoice.IsAnnual ? plan.AnnualPrice : plan.MonthlyPrice;
        var chargesAmount = Math.Min(amount * _chargesPercentage / 100m, _chargesCap);
        var total = amount + chargesAmount;

        var description = $"Tier {(int)plan.Tier} - {plan.Name} - subscription payment ({now.ToString(_dateFormat)} - {endDate.ToString(_dateFormat)})";

        var payload = new Dictionary<string, object>
        {
            { "status", "unpaid" },
            { "description", description },
            { "payment_amount", amount },
            { "charges_description", _chargesDescription },
            { "charges_amount", chargesAmount },
            { "total", total },
            { "invoice_date", now.ToString(_dateFormat) },
            { "due_date", now.ToString(_dateFormat) },
            { "invoice_id", invoice.Id }
        };

        await _notificationClient.SendNotificationAsync(new NotificationRequest
        {
            Template = "invoice",
            Channel = "email",
            Recipient = invoice.Email,
            Payload = payload
        });
    }
}
