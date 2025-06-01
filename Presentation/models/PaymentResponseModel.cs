namespace Presentation.models
{
    public class PaymentResponseModel
    {
        public string PaymentId { get; set; } = null!;
        public string EventId { get; set; } = null!;
        public DateTime BookingDate { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }

        public BookedByModel BookedBy { get; set; } = null!;
        public List<TicketModel> Tickets { get; set; } = new();
    }
}
