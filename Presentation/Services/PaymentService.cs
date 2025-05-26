//using Microsoft.EntityFrameworkCore;
//using Presentation.Data;
//using Presentation.models;
//using Stripe;
//using Stripe.Checkout;
//using System.Text;
//using System.Text.Json;

//namespace Presentation.Services;

//public class PaymentService : IPaymentService
//{
//    private readonly IConfiguration _config;
//    private readonly DataContext _context;
//    private readonly HttpClient _http;

//    public PaymentService(IConfiguration config, DataContext context)
//    {
//        _config = config;
//        _context = context;
//        _http = new HttpClient();
//        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
//    }

//    public async Task<string> CreateStripeCheckoutSessionAsync(PaymentRequest request)
//    {
//        var options = new SessionCreateOptions
//        {
//            PaymentMethodTypes = ["card"],
//            LineItems =
//            [
//                new SessionLineItemOptions
//            {
//                PriceData = new SessionLineItemPriceDataOptions
//                {
//                    Currency = "sek",
//                    UnitAmount = (long)(request.Amount * 100),
//                    ProductData = new SessionLineItemPriceDataProductDataOptions
//                    {
//                        Name = $"Event #{request.EventId}"
//                    }
//                },
//                Quantity = 1
//            }
//            ],
//            Mode = "payment",
//            SuccessUrl = _config["Stripe:SuccessUrl"],
//            CancelUrl = _config["Stripe:CancelUrl"],
//            Metadata = new Dictionary<string, string>
//        {
//            { "eventId", request.EventId },
//            { "userId", request.UserId },
//            { "firstName", request.FirstName },
//            { "lastName", request.LastName },
//            { "phoneNumber", request.PhoneNumber },
//            { "amount", request.Amount.ToString("F2") }
//        }
//        };

//        var session = await new SessionService().CreateAsync(options);
//        return session.Id;
//    }


//    public async Task<List<PaymentResponseModel>> GetPaymentsAsync(string userId, bool isAdmin)
//    {
//        var query = _context.Payments.Include(p => p.User).AsQueryable();

//        if (!isAdmin)
//        {
//            query = query.Where(p => p.UserId == userId);
//        }

//        return await query
//            .OrderByDescending(p => p.BookingDate)
//         .Select(p => new PaymentResponseModel
//         {
//             PaymentId = p.PaymentId,
//             EventId = p.EventId,
//             BookingDate = p.BookingDate,
//             Amount = p.Amount,
//             IsPaid = p.IsPaid,
//             BookedBy = new BookedByModel
//             {
//                 FirstName = p.User.FirstName,
//                 LastName = p.User.LastName,
//                 PhoneNumber = p.User.PhoneNumber
//             },
//             Tickets = p.Tickets.Select(t => new TicketModel
//             {
//                 FirstName = t.FirstName,
//                 LastName = t.LastName,
//                 PhoneNumber = t.PhoneNumber
//             }).ToList()
//         }).ToListAsync();
//    }

//    public async Task<bool> MarkPaymentAsPaidAndBookAsync(string json, string stripeSignature)
//    {
//        var secret = _config["Stripe:WebhookSecret"];
//        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, secret);

//        if (stripeEvent.Type != "checkout.session.completed") return false;

//        var session = stripeEvent.Data.Object as Session;
//        if (session?.Metadata == null) return false;

//        var userId = session.Metadata["userId"];
//        var firstName = session.Metadata["firstName"];
//        var lastName = session.Metadata["lastName"];
//        var phoneNumber = session.Metadata["phoneNumber"];
//        var eventId = session.Metadata["eventId"];
//        var amount = decimal.Parse(session.Metadata["amount"]);

//        var user = await _context.Users.FindAsync(userId);
//        if (user == null)
//        {
//            user = new UserEntity
//            {
//                UserId = userId,
//                FirstName = firstName,
//                LastName = lastName,
//                PhoneNumber = phoneNumber
//            };
//            _context.Users.Add(user);
//        }

//        var payment = new PaymentEntity
//        {
//            PaymentId = Guid.NewGuid().ToString(),
//            EventId = eventId,
//            UserId = userId,
//            StripeSessionId = session.Id,
//            BookingDate = DateTime.UtcNow,
//            Amount = amount,
//            IsPaid = true
//        };

//        _context.Payments.Add(payment);
//        await _context.SaveChangesAsync();

//        var bookingRequest = new
//        {
//            userId,
//            eventId
//        };

//        var content = new StringContent(JsonSerializer.Serialize(bookingRequest), Encoding.UTF8, "application/json");
//        var response = await _http.PostAsync("https://bookeventprovider.azurewebsites.net/api/booking", content);

//        return response.IsSuccessStatusCode;
//    }
//}