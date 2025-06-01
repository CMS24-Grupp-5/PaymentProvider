using Microsoft.AspNetCore.Mvc;

namespace Presentation.Services
{
    public interface IWebhookService
    {
        Task<IActionResult> HandleStripeWebhookAsync(HttpRequest request);
    }
}