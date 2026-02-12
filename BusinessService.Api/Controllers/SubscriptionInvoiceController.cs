using BusinessService.Application.DTOs.Subscription;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionInvoiceController : ControllerBase
{
    private readonly ISubscriptionInvoiceService _invoiceService;
    private readonly ILogger<SubscriptionInvoiceController> _logger;

    public SubscriptionInvoiceController(
        ISubscriptionInvoiceService invoiceService,
        ILogger<SubscriptionInvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Initiate a checkout for a subscription
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponse>> Checkout([FromBody] CheckoutRequest request)
    {
        try
        {
            var result = await _invoiceService.CheckoutAsync(request);
            return Ok(result);
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (SubscriptionNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (PaymentInitiationException ex)
        {
            _logger.LogError(ex, "Payment initiation failed for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checkout for business {BusinessId}", request.BusinessId);
            return StatusCode(500, new { message = "An error occurred during checkout" });
        }
    }

    /// <summary>
    /// Get an invoice by ID
    /// </summary>
    [HttpGet("invoice/{invoiceId:guid}")]
    public async Task<ActionResult<SubscriptionInvoiceDto>> GetInvoice(Guid invoiceId)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceAsync(invoiceId);
            if (invoice == null)
                return NotFound(new { message = "Invoice not found" });
            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new { message = "An error occurred while fetching the invoice" });
        }
    }
}
