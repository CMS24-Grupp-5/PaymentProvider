using Microsoft.AspNetCore.Mvc;
using Presentation.Extentions;
using Presentation.models;
using Presentation.Services;

namespace Presentation.Controllers
{
    /// <summary>
    /// API-kontroller för hantering av betalningar via Stripe.
    /// Skyddas med API-nyckel via <see cref="ApiKeyAttribute"/>.
    /// </summary>
    [ApiKey]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Skapar en ny Stripe Checkout-session för en betalning.
        /// </summary>
        /// <param name="request">Information om användare, event och belopp.</param>
        /// <returns>Session ID för Stripe Checkout.</returns>
        /// <response code="200">Session ID returneras vid lyckad skapelse.</response>
        /// <response code="400">Om begäran är ogiltig.</response>
        [HttpPost("create-checkout-session")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] PaymentRequest request)
        {
            var sessionId = await _paymentService.CreateCheckoutSessionAsync(request);
            return Ok(new { sessionId });
        }

        /// <summary>
        /// Hämtar betalningar för en specifik användare eller alla om admin.
        /// </summary>
        /// <param name="userId">Användarens ID.</param>
        /// <param name="isAdmin">Om användaren är administratör (hämtar alla).</param>
        /// <returns>En lista med betalningsinformation.</returns>
        /// <response code="200">Returnerar betalningslista.</response>
        /// <response code="400">Om användar-ID saknas.</response>
        [HttpGet("GetPayments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPayments([FromQuery] string userId, [FromQuery] bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("User ID is required");

            var payments = await _paymentService.GetPaymentsAsync(userId, isAdmin);
            return Ok(payments);
        }
    }
}
