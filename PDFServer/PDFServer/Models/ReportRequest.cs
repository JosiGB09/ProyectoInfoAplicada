namespace PDFServer.Models
{
    public class ReportRequest
    {
        public int CustomerId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ReportRequest() { }
        public ReportRequest(int customerId, string correlationId, DateTime startDate, DateTime endDate)
        {
            CustomerId = customerId;
            CorrelationId = correlationId;
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
