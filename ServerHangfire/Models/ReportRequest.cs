namespace ServerHangfire.Models
{
    public class ReportRequest
    {
        public int CustomerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? CorrelationId { get; set; }
    }
}
