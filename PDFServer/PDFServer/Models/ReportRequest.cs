using System.ComponentModel.DataAnnotations;

namespace PDFServer.Models
{
    public class ReportRequest
    {
        public required int CustomerId { get; set; }
        public required string CorrelationId { get; set; }
        public required DateTime StartDate { get; set; }
        public required DateTime EndDate { get; set; }

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
