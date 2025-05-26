using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Presentation.models;
using Stripe.Checkout;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] PaymentRequest request)
    {
        var existingUser = await _context.Users.FindAsync(request.UserId);
        if (existingUser == null)
        {
            existingUser = new UserEntity
            {
                UserId = request.UserId,
                FirstName = request.BookedBy.FirstName,
                LastName = request.BookedBy.LastName,
                PhoneNumber = request.BookedBy.PhoneNumber,
                  Email = request.BookedBy.Email
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

        // Fix: Remove the invalid reference to 'tickets' and directly assign the result of the LINQ query to payment.Tickets
        payment.Tickets = request.Tickets.Select(t => new TicketEntity
        {
            PaymentId = payment.PaymentId,
            FirstName = t.FirstName,
            LastName = t.LastName,
            PhoneNumber = t.PhoneNumber,
            UserId = request.UserId
        }).ToList();

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
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
            },
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
    .Include(p => p.Tickets)
    .OrderByDescending(p => p.BookingDate)
    .Select(p => new PaymentResponseModel
    {
        PaymentId = p.PaymentId,
        EventId = p.EventId,
        BookingDate = p.BookingDate,
        Amount = p.Amount,
        IsPaid = p.IsPaid,
        BookedBy = new BookedByModel
        {
            FirstName = p.User.FirstName,
            LastName = p.User.LastName,
            PhoneNumber = p.User.PhoneNumber
        },
        Tickets = p.Tickets.Select(t => new TicketModel
        {
            FirstName = t.FirstName,
            LastName = t.LastName,
            PhoneNumber = t.PhoneNumber
        }).ToList()
    }).ToListAsync();

        return Ok(payments);
    }
}
