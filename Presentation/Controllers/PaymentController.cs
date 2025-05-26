using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Stripe.Checkout;


namespace Presentation.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly DataContext _context;

    public PaymentController(IConfiguration configuration, DataContext context)
    {
        _configuration = configuration;
        _context = context;
        Stripe.StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public class PaymentRequest
    {
        public string EventId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public decimal Amount { get; set; }
    }

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] PaymentRequest request)
    {

        var existingUser = await _context.Users.FindAsync(request.UserId);
        if (existingUser == null)
        {
            existingUser = new UserEntity
            {
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber
            };

            _context.Users.Add(existingUser);
        }

        var payment = new PaymentEntity
        {
            PaymentId = Guid.NewGuid().ToString(),
            EventId = request.EventId,
            BookingDate = DateTime.UtcNow,
            StripeSessionId = "TEMP",
            IsPaid = false,
            UserId = request.UserId,
            Amount = request.Amount
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "sek",
                        UnitAmount = (long)(request.Amount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Event #{request.EventId}"
                        }
                    },
                    Quantity = 1
                }
            ],
            Mode = "payment",
            SuccessUrl = _configuration["Stripe:SuccessUrl"],
            CancelUrl = _configuration["Stripe:CancelUrl"],
            Metadata = new Dictionary<string, string>
            {
                { "paymentId", payment.PaymentId },
                { "userId", request.UserId },
                { "eventId", request.EventId },
                { "amount", request.Amount.ToString("F2") }
            }
        };

        var sessionService = new SessionService();
        var session = sessionService.Create(options);

        payment.StripeSessionId = session.Id;
        await _context.SaveChangesAsync();

        return Ok(new { sessionId = session.Id });
    }


    [HttpGet("GetPayments")]
    public async Task<IActionResult> GetPayments([FromQuery] string userId, [FromQuery] bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("User ID is required");

        var query = _context.Payments
            .Include(p => p.User)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(p => p.UserId == userId);
        }

        var payments = await query
            .OrderByDescending(p => p.BookingDate)
            .Select(p => new
            {
                p.PaymentId,
                p.EventId,
                p.BookingDate,
                p.Amount,
                p.IsPaid,
                p.User.FirstName,
                p.User.LastName,
                p.User.PhoneNumber
            })
            .ToListAsync();

        return Ok(payments);
    }
}
