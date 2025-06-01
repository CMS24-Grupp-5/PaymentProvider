using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Presentation.Data;
using Stripe;
using Stripe.Checkout;
using System.Text;
using System.Text.Json;

namespace Presentation.Services
{
    public class WebhookService(IConfiguration config, DataContext context) : IWebhookService
    {
        private readonly IConfiguration _config = config;
        private readonly DataContext _context = context;
        private readonly HttpClient _http = new HttpClient();

        public async Task<IActionResult> HandleStripeWebhookAsync(HttpRequest request)
        {
            var json = await new StreamReader(request.Body).ReadToEndAsync();
            var stripeSignature = request.Headers["Stripe-Signature"];
            var secret = _config["Stripe:WebhookSecret"];

            if (string.IsNullOrEmpty(secret) || StringValues.IsNullOrEmpty(stripeSignature))
            {
                Console.WriteLine("WebhookSecret eller Stripe-Signature saknas.");
                return new StatusCodeResult(400);
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, secret);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Webhook verification failed: {ex.Message}");
                return new BadRequestResult();
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                var metadata = session?.Metadata ?? new Dictionary<string, string>();

                var paymentId = metadata.GetValueOrDefault("paymentId");
                var userId = metadata.GetValueOrDefault("userId");
                var eventId = metadata.GetValueOrDefault("eventId");

                if (string.IsNullOrWhiteSpace(paymentId))
                {
                    Console.WriteLine("[WARN] paymentId saknas, men fortsätter.");
                }

                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
                if (payment != null)
                {
                    payment.IsPaid = true;
                    await _context.SaveChangesAsync();

                    var bookingRequest = new { userId, eventId };
                    var content = new StringContent(JsonSerializer.Serialize(bookingRequest), Encoding.UTF8, "application/json");

                    var response = await _http.PostAsync("https://bookeventprovider.azurewebsites.net/api/booking", content);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[SUCCESS] Bokning skapad för userId: {userId}, eventId: {eventId}");
                    }
                }
                else
                {
                    Console.WriteLine("[INFO] Inget payment-objekt hittades, event loggas bara.");
                }
            }

            return new OkResult();
        }
    }
}
