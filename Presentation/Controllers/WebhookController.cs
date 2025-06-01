using Microsoft.AspNetCore.Mvc;
using Presentation.Extentions;
using Presentation.Services;

namespace Presentation.Controllers
{
    /// <summary>
    /// API-kontroller som tar emot Stripe-webhook-anrop efter betalning.
    /// </summary>
    [ApiKey]
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController(IWebhookService webhookService) : ControllerBase
    {
        private readonly IWebhookService _webhookService = webhookService;

        /// <summary>
        /// Hanterar inkommande Stripe-webhooks, t.ex. checkout.session.completed.
        /// </summary>
        /// <returns>HTTP 200 vid lyckad hantering, annars 400.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StripeWebhook()
        {
            return await _webhookService.HandleStripeWebhookAsync(Request);
        }
    }
}
