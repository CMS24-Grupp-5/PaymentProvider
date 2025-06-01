using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Presentation.models;
using Stripe.Checkout;

namespace Presentation.Services;

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _config;
    private readonly DataContext _context;

    public PaymentService(IConfiguration config, DataContext context)
    {
        _config = config;
        _context = context;
        Stripe.StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(PaymentRequest request)
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

        var paymentId = Guid.NewGuid().ToString(); 
        var payment = new PaymentEntity
        {
            PaymentId = paymentId, 
            EventId = request.EventId,
            BookingDate = DateTime.UtcNow,
            StripeSessionId = "TEMP",
            IsPaid = false,
            UserId = request.UserId,
            Amount = request.Amount,
            Tickets = request.Tickets.Select(t => new TicketEntity
            {
                PaymentId = paymentId, 
                FirstName = t.FirstName,
                LastName = t.LastName,
                PhoneNumber = t.PhoneNumber,
                UserId = request.UserId
            }).ToList()
        };

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
            SuccessUrl = _config["Stripe:SuccessUrl"],
            CancelUrl = _config["Stripe:CancelUrl"],
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

        return session.Id;
    }

    public async Task<List<PaymentResponseModel>> GetPaymentsAsync(string userId, bool isAdmin)
    {
        var query = _context.Payments.Include(p => p.User).Include(p => p.Tickets).AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(p => p.UserId == userId);
        }

        return await query.OrderByDescending(p => p.BookingDate).Select(p => new PaymentResponseModel
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
    }
}