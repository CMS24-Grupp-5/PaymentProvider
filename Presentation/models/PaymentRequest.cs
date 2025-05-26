namespace Presentation.models
{ 
        public class PaymentRequest
        {
        public string EventId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public BookedByModel BookedBy { get; set; } = null!;
        public List<TicketModel> Tickets { get; set; } = new();
    }
    
}
