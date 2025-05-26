using Presentation.models;

namespace Presentation.Services
{
    public interface IPaymentService
    {
        Task<string> CreateStripeCheckoutSessionAsync(PaymentRequest request);
        Task<List<PaymentResponseModel>> GetPaymentsAsync(string userId, bool isAdmin);
        Task<bool> MarkPaymentAsPaidAndBookAsync(string json, string stripeSignature);
    }
}