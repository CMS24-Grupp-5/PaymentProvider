using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presentation.Data;
using Stripe;
using Stripe.Checkout;
using System.Text;
using System.Text.Json;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly DataContext _context;
        private readonly HttpClient _http;

        public WebhookController(IConfiguration config, DataContext context)
        {
            _config = config;
            _context = context;
            _http = new HttpClient();
        }

        [HttpPost]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];
            var secret = _config["Stripe:WebhookSecret"];

            if (string.IsNullOrEmpty(secret))
            {
                Console.WriteLine("WebhookSecret saknas i konfigurationen.");
                return StatusCode(500, "Webhook secret not configured.");
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, secret);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Webhook verification failed: {ex.Message}");
                return BadRequest();
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session?.Metadata == null)
                {
                    Console.WriteLine("[ERROR] Metadata saknas i checkout-session.");
                    return BadRequest();
                }

                string? paymentId = session.Metadata.GetValueOrDefault("paymentId");
                string? userId = session.Metadata.GetValueOrDefault("userId");
                string? eventId = session.Metadata.GetValueOrDefault("eventId");

                if (string.IsNullOrWhiteSpace(paymentId))
                {
                    Console.WriteLine("[ERROR] paymentId saknas i metadata.");
                    return BadRequest();
                }

                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
                if (payment == null)
                {
                    Console.WriteLine($"[ERROR] Ingen payment hittades för ID: {paymentId}");
                    return NotFound();
                }

                payment.IsPaid = true;
                await _context.SaveChangesAsync();

                var bookingRequest = new
                {
                    userId = userId,
                    eventId = eventId
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(bookingRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _http.PostAsync("https://bookeventprovider.azurewebsites.net/api/booking", content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ERROR] Kunde inte skapa bokning i BookEventProvider. Status: {response.StatusCode}");
                }
                else
                {
                    Console.WriteLine($"[SUCCESS] Bokning skapad för userId: {userId}, eventId: {eventId}");
                }
            }

            return Ok();
        }
    }
}
