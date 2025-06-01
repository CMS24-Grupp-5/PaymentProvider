using Presentation.models;

namespace Presentation.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(PaymentRequest request);
        Task<List<PaymentResponseModel>> GetPaymentsAsync(string userId, bool isAdmin);
    }
}