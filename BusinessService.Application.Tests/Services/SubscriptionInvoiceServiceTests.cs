using BusinessService.Application.DTOs.Subscription;
using BusinessService.Application.Interfaces;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BusinessService.Application.Tests.Services;

[TestFixture]
public class SubscriptionInvoiceServiceTests
{
    private Mock<ISubscriptionInvoiceRepository> _invoiceRepoMock = null!;
    private Mock<ISubscriptionPlanRepository> _planRepoMock = null!;
    private Mock<IBusinessRepository> _businessRepoMock = null!;
    private Mock<IPaymentInitiator> _paymentInitiatorMock = null!;
    private Mock<INotificationServiceClient> _notificationClientMock = null!;
    private Mock<ILogger<SubscriptionInvoiceService>> _loggerMock = null!;
    private IConfiguration _configuration = null!;
    private SubscriptionInvoiceService _service = null!;

    [SetUp]
    public void Setup()
    {
        _invoiceRepoMock = new Mock<ISubscriptionInvoiceRepository>();
        _planRepoMock = new Mock<ISubscriptionPlanRepository>();
        _businessRepoMock = new Mock<IBusinessRepository>();
        _paymentInitiatorMock = new Mock<IPaymentInitiator>();
        _notificationClientMock = new Mock<INotificationServiceClient>();
        _loggerMock = new Mock<ILogger<SubscriptionInvoiceService>>();

        var configData = new Dictionary<string, string?>
        {
            { "Invoice:DateFormat", "dd/MM/yyyy" },
            { "Invoice:ChargesDescription", "Paystack Transaction Fee - (1.5%)" },
            { "Invoice:ChargesPercentage", "1.5" },
            { "Invoice:ChargesCap", "2000" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _service = new SubscriptionInvoiceService(
            _invoiceRepoMock.Object,
            _planRepoMock.Object,
            _businessRepoMock.Object,
            _paymentInitiatorMock.Object,
            _notificationClientMock.Object,
            _configuration,
            _loggerMock.Object
        );
    }

    // ========== Checkout Tests ==========

    [Test]
    public async Task CheckoutAsync_ShouldReturnInvoiceId_WhenPaymentInitiationSucceeds()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var plan = new SubscriptionPlan
        {
            Id = subscriptionId,
            Name = "Premium",
            Tier = SubscriptionTier.Premium,
            MonthlyPrice = 15000m,
            AnnualPrice = 150000m
        };

        var request = new CheckoutRequest
        {
            Email = "test@example.com",
            BusinessId = businessId,
            SubscriptionId = subscriptionId,
            IsAnnual = false,
            Platform = "paystack"
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _planRepoMock.Setup(r => r.FindByIdAsync(subscriptionId)).ReturnsAsync(plan);
        _paymentInitiatorMock.Setup(p => p.InitiatePaymentAsync(It.IsAny<PaymentInitiationRequest>()))
            .ReturnsAsync(new PaymentInitiationResult(true, "ref_123", "https://paystack.co/pay/abc", null));
        _invoiceRepoMock.Setup(r => r.AddAsync(It.IsAny<SubscriptionInvoice>()))
            .ReturnsAsync((SubscriptionInvoice inv) => inv);

        // Act
        var result = await _service.CheckoutAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Transaction initiated");
        result.Invoice.Should().NotBeEmpty();

        _invoiceRepoMock.Verify(r => r.AddAsync(It.Is<SubscriptionInvoice>(inv =>
            inv.Email == "test@example.com" &&
            inv.BusinessId == businessId &&
            inv.SubscriptionId == subscriptionId &&
            inv.Platform == "paystack" &&
            inv.Reference == "ref_123" &&
            inv.PaymentUrl == "https://paystack.co/pay/abc" &&
            inv.Status == "unpaid"
        )), Times.Once);
    }

    [Test]
    public async Task CheckoutAsync_ShouldUseAnnualPrice_WhenIsAnnualTrue()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var plan = new SubscriptionPlan
        {
            Id = subscriptionId,
            Name = "Premium",
            Tier = SubscriptionTier.Premium,
            MonthlyPrice = 15000m,
            AnnualPrice = 150000m
        };

        var request = new CheckoutRequest
        {
            Email = "test@example.com",
            BusinessId = businessId,
            SubscriptionId = subscriptionId,
            IsAnnual = true
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _planRepoMock.Setup(r => r.FindByIdAsync(subscriptionId)).ReturnsAsync(plan);
        _paymentInitiatorMock.Setup(p => p.InitiatePaymentAsync(It.IsAny<PaymentInitiationRequest>()))
            .ReturnsAsync(new PaymentInitiationResult(true, "ref_456", "https://paystack.co/pay/def", null));
        _invoiceRepoMock.Setup(r => r.AddAsync(It.IsAny<SubscriptionInvoice>()))
            .ReturnsAsync((SubscriptionInvoice inv) => inv);

        // Act
        var result = await _service.CheckoutAsync(request);

        // Assert
        // 150000 base + 2000 charges (1.5% = 2250, capped at 2000) + 11400 VAT (7.5% of 152000) = 163400
        result.Should().NotBeNull();
        _paymentInitiatorMock.Verify(p => p.InitiatePaymentAsync(It.Is<PaymentInitiationRequest>(
            r => r.Amount == 163400m
        )), Times.Once);
    }

    [Test]
    public async Task CheckoutAsync_ShouldDefaultPlatformToPaystack_WhenPlatformNull()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var plan = new SubscriptionPlan
        {
            Id = subscriptionId,
            Name = "Basic",
            Tier = SubscriptionTier.Basic,
            MonthlyPrice = 0m,
            AnnualPrice = 0m
        };

        var request = new CheckoutRequest
        {
            Email = "test@example.com",
            BusinessId = businessId,
            SubscriptionId = subscriptionId,
            IsAnnual = false,
            Platform = null
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _planRepoMock.Setup(r => r.FindByIdAsync(subscriptionId)).ReturnsAsync(plan);
        _paymentInitiatorMock.Setup(p => p.InitiatePaymentAsync(It.IsAny<PaymentInitiationRequest>()))
            .ReturnsAsync(new PaymentInitiationResult(true, "ref_789", "https://paystack.co/pay/ghi", null));
        _invoiceRepoMock.Setup(r => r.AddAsync(It.IsAny<SubscriptionInvoice>()))
            .ReturnsAsync((SubscriptionInvoice inv) => inv);

        // Act
        await _service.CheckoutAsync(request);

        // Assert
        _invoiceRepoMock.Verify(r => r.AddAsync(It.Is<SubscriptionInvoice>(inv =>
            inv.Platform == "paystack"
        )), Times.Once);
    }

    [Test]
    public void CheckoutAsync_ShouldThrowBusinessNotFoundException_WhenBusinessNotFound()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            Email = "test@example.com",
            BusinessId = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            IsAnnual = false
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(request.BusinessId)).ReturnsAsync((Business?)null);

        // Act
        Func<Task> act = async () => await _service.CheckoutAsync(request);

        // Assert
        act.Should().ThrowAsync<BusinessNotFoundException>();
    }

    [Test]
    public void CheckoutAsync_ShouldThrowSubscriptionNotFoundException_WhenPlanNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };

        var request = new CheckoutRequest
        {
            Email = "test@example.com",
            BusinessId = businessId,
            SubscriptionId = Guid.NewGuid(),
            IsAnnual = false
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _planRepoMock.Setup(r => r.FindByIdAsync(request.SubscriptionId)).ReturnsAsync((SubscriptionPlan?)null);

        // Act
        Func<Task> act = async () => await _service.CheckoutAsync(request);

        // Assert
        act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    [Test]
    public void CheckoutAsync_ShouldThrowPaymentInitiationException_WhenPaymentFails()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Test Business" };
        var plan = new SubscriptionPlan
        {
            Id = subscriptionId,
            Name = "Premium",
            Tier = SubscriptionTier.Premium,
            MonthlyPrice = 15000m,
            AnnualPrice = 150000m
        };

        var request = new CheckoutRequest
        {
            Email = "test@example.com",
            BusinessId = businessId,
            SubscriptionId = subscriptionId,
            IsAnnual = false
        };

        _businessRepoMock.Setup(r => r.FindByIdAsync(businessId)).ReturnsAsync(business);
        _planRepoMock.Setup(r => r.FindByIdAsync(subscriptionId)).ReturnsAsync(plan);
        _paymentInitiatorMock.Setup(p => p.InitiatePaymentAsync(It.IsAny<PaymentInitiationRequest>()))
            .ReturnsAsync(new PaymentInitiationResult(false, null, null, "Payment gateway error"));

        // Act
        Func<Task> act = async () => await _service.CheckoutAsync(request);

        // Assert
        act.Should().ThrowAsync<PaymentInitiationException>()
            .WithMessage("Payment gateway error");
    }

    // ========== GetInvoice Tests ==========

    [Test]
    public async Task GetInvoiceAsync_ShouldReturnInvoiceWithPlanSummary_WhenInvoiceExists()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var invoice = new SubscriptionInvoice
        {
            Id = invoiceId,
            IsAnnual = false,
            BusinessId = Guid.NewGuid(),
            Platform = "paystack",
            SubscriptionId = subscriptionId,
            PaymentUrl = "https://paystack.co/pay/abc",
            Email = "test@example.com",
            Reference = "ref_123",
            Status = "unpaid",
            CreatedAt = DateTime.UtcNow
        };

        var plan = new SubscriptionPlan
        {
            Id = subscriptionId,
            Name = "Premium",
            Tier = SubscriptionTier.Premium,
            Description = "Premium plan",
            MonthlyPrice = 15000m,
            AnnualPrice = 150000m
        };

        _invoiceRepoMock.Setup(r => r.FindByIdAsync(invoiceId)).ReturnsAsync(invoice);
        _planRepoMock.Setup(r => r.FindByIdAsync(subscriptionId)).ReturnsAsync(plan);

        // Act
        var result = await _service.GetInvoiceAsync(invoiceId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(invoiceId);
        result.Email.Should().Be("test@example.com");
        result.Status.Should().Be("unpaid");
        result.Subscription.Should().NotBeNull();
        result.Subscription!.Name.Should().Be("Premium");
        result.Subscription.Tier.Should().Be("Premium");
        result.Subscription.MonthlyPrice.Should().Be(15000m);
        result.Subscription.AnnualPrice.Should().Be(150000m);
    }

    [Test]
    public async Task GetInvoiceAsync_ShouldReturnNull_WhenInvoiceNotFound()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        _invoiceRepoMock.Setup(r => r.FindByIdAsync(invoiceId)).ReturnsAsync((SubscriptionInvoice?)null);

        // Act
        var result = await _service.GetInvoiceAsync(invoiceId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetInvoiceAsync_ShouldReturnNullSubscription_WhenPlanNotFound()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var invoice = new SubscriptionInvoice
        {
            Id = invoiceId,
            IsAnnual = false,
            BusinessId = Guid.NewGuid(),
            Platform = "paystack",
            SubscriptionId = subscriptionId,
            Email = "test@example.com",
            Status = "unpaid",
            CreatedAt = DateTime.UtcNow
        };

        _invoiceRepoMock.Setup(r => r.FindByIdAsync(invoiceId)).ReturnsAsync(invoice);
        _planRepoMock.Setup(r => r.FindByIdAsync(subscriptionId)).ReturnsAsync((SubscriptionPlan?)null);

        // Act
        var result = await _service.GetInvoiceAsync(invoiceId);

        // Assert
        result.Should().NotBeNull();
        result!.Subscription.Should().BeNull();
    }
}
