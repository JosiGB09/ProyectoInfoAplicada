namespace PDFServer.Models
{
    public class LogEvent
    {
        public string CorrelationId { get; set; }
        public string Service { get; set; }
        public string Endpoint { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Payload { get; set; }
        public string FileName { get; set; }
        public bool Success { get; set; }
        

    }
}
